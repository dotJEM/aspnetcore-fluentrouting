using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.AspNetCore.FluentRouting.Builders;
using DotJEM.AspNetCore.FluentRouting.Builders.RouteObjects;
using DotJEM.AspNetCore.FluentRouting.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace DotJEM.AspNetCore.FluentRouting.Invoker
{
    public class FluentActionSelector : IActionSelector
    {
        //TODO: Can we avoid so much code duplication here - most is copied from the standard MVC selector?...
        //private readonly ActionSelector selector;
        private readonly IFluentActionDescriptorCache cache;
        private readonly ActionConstraintCache actionConstraintCache;

        public FluentActionSelector(IFluentActionDescriptorCache cache, ActionConstraintCache actionConstraintCache)
        {
            this.cache = cache;
            this.actionConstraintCache = actionConstraintCache;
        }

        public IReadOnlyList<ActionDescriptor> SelectCandidates(RouteContext context)
        {
            List<ActionDescriptor> candidates = new List<ActionDescriptor>();
            foreach (IRouter router in context.RouteData.Routers)
            {
                candidates.AddRange(cache.Lookup(router as ControllerRoute));
                candidates.AddRange(cache.Lookup(router as LambdaRoute));
            }
            return candidates;
        }

        public ActionDescriptor SelectBestCandidate(RouteContext context, IReadOnlyList<ActionDescriptor> candidates)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));

            IReadOnlyList<ActionDescriptor> matches = EvaluateActionConstraints(context, candidates);
            if (matches.Count == 1)
                return matches.Single();

            matches = EvaluateActionParameters(context, matches);

            if (matches == null || matches.Count == 0)
                return null;

            if (matches.Count == 1)
            {
                ActionDescriptor selectedAction = matches.Single();
                return selectedAction;
            }

            string actionNames = string.Join(
                Environment.NewLine,
                matches.Select(a => a.DisplayName));

            //_logger.AmbiguousActions(actionNames);
            throw new AmbiguousActionException($"Resources.FormatDefaultActionSelector_AmbiguousActions(Environment.NewLine,{actionNames})");
        }

        private IReadOnlyList<ActionDescriptor> EvaluateActionParameters(RouteContext context, IReadOnlyList<ActionDescriptor> candidates)
        {
            List<ActionDescriptor> matches = new List<ActionDescriptor>();
            int paramCount = 0;
            foreach (ActionDescriptor candidate in candidates.OrderByDescending(c => c.Parameters.Count))
            {
                if (candidate.Parameters.Count < paramCount && matches.Count > 0)
                    return matches;

                if (SatisfiesParameters(context, candidate))
                {
                    paramCount = candidate.Parameters.Count;
                    matches.Add(candidate);
                }
            }
            return matches;
        }

        private bool SatisfiesParameters(RouteContext context, ActionDescriptor candidate)
        {
            foreach (ParameterDescriptor parameter in candidate.Parameters)
            {
                //TODO: Foreach parameter => binding source => has parameter.
                //      Also we should be able to get the information out of the binding source it self weather it can succced or not.
                //      Otherwise, how could people implement their own binding sources?
                //
                //      Aditional Info: So the Binding Source will actually produce a Binding Result after we have attempted to bind.
                //      However this happens much later in the process so that result can't be used here...
                //      Ofc. this could be solved by attempting to bind early, but obviously that would be a heavy process as we might end addempting to bind
                //      On allot of models before we actually find a candidate match, so...
                
                HashSet<string> valuenames = new HashSet<string>(Concat(
                    context.RouteData.DataTokens.Keys,
                    context.RouteData.Values.Keys,
                    //context.HttpContext.Request.Form.Keys, //TODO: depends on contentType
                    context.HttpContext.Request.Headers.Keys,
                    context.HttpContext.Request.Query.Keys
                    ));

                BindingInfo info = parameter.BindingInfo;
                BindingSource source = info?.BindingSource;

                switch (source?.Id)
                {
                    case "Form":
                    case "Header":
                    case "Path": //FromRoute
                    case "Query":
                    case null: //TODO: If this is a Post, Put etc route, then the Body is actually also a candidate
                        if (!valuenames.Contains(parameter.Name))
                            return false;
                        break;
                    case "Body":
                    case "Custom":
                    case "ModelBinding":
                    case "Services":
                    case "Special":
                    case "FormFile":
                        //Note: No specific binding info, we have to try them all.
                        break;
                }

            }
            return true;
        }

        private IEnumerable<string> Concat(params IEnumerable<string>[] sources)
        {
            return sources.SelectMany(s => s);
        }

        private IReadOnlyList<ActionDescriptor> EvaluateActionConstraints(RouteContext context, IReadOnlyList<ActionDescriptor> actions)
        {
            //TODO: Convert back to for:i to avoid allocations - for now readability!
            List<ActionSelectorCandidate> candidates = (
                from action in actions
                let constraints = actionConstraintCache.GetActionConstraints(context.HttpContext, action)
                select new ActionSelectorCandidate(action, constraints)).ToList();

            // Perf: Avoid allocations
            IReadOnlyList<ActionSelectorCandidate> matches = EvaluateActionConstraintsCore(context, candidates, null);

            //TODO: Convert back to for:i to avoid allocations - for now readability!
            return matches?.Select(candidate => candidate.Action).ToList();
        }

        private IReadOnlyList<ActionSelectorCandidate> EvaluateActionConstraintsCore(RouteContext context, IReadOnlyList<ActionSelectorCandidate> candidates, int? startingOrder)
        {
            // Find the next group of constraints to process. This will be the lowest value of
            // order that is higher than startingOrder.
            int? order = null;

            //TODO: Convert back to for:i to avoid allocations - for now readability!
            foreach (ActionSelectorCandidate candidate in candidates)
            {
                if (candidate.Constraints == null)
                    continue;
                //TODO: Convert back to for:i to avoid allocations - for now readability!
                foreach (IActionConstraint constraint in candidate.Constraints)
                {
                    if (startingOrder != null && !(constraint.Order > startingOrder) || order != null && !(constraint.Order < order))
                        continue;
                    order = constraint.Order;
                }
            }

            // If we don't find a 'next' then there's nothing left to do.
            if (order == null)
                return candidates;

            // Since we have a constraint to process, bisect the set of actions into those with and without a
            // constraint for the 'current order'.
            List<ActionSelectorCandidate> actionsWithConstraint = new List<ActionSelectorCandidate>();
            List<ActionSelectorCandidate> actionsWithoutConstraint = new List<ActionSelectorCandidate>();

            ActionConstraintContext constraintContext = new ActionConstraintContext();
            constraintContext.Candidates = candidates;
            constraintContext.RouteContext = context;

            //TODO: Convert back to for:i to avoid allocations - for now readability!
            foreach (ActionSelectorCandidate candidate in candidates)
            {
                bool isMatch = true;
                bool foundMatchingConstraint = false;

                if (candidate.Constraints != null)
                {
                    constraintContext.CurrentCandidate = candidate;
                    //TODO: Convert back to for:i to avoid allocations - for now readability!
                    foreach (IActionConstraint constraint in candidate.Constraints)
                    {
                        if (constraint.Order != order)
                            continue;

                        foundMatchingConstraint = true;

                        if (constraint.Accept(constraintContext))
                            continue;

                        isMatch = false;
                        //_logger.ConstraintMismatch(
                        //    candidate.Action.DisplayName,
                        //    candidate.Action.Id,
                        //    constraint);
                        break;
                    }
                }

                if (isMatch && foundMatchingConstraint)
                    actionsWithConstraint.Add(candidate);
                else if (isMatch)
                    actionsWithoutConstraint.Add(candidate);
            }

            // If we have matches with constraints, those are 'better' so try to keep processing those
            if (actionsWithConstraint.Count > 0)
            {
                IReadOnlyList<ActionSelectorCandidate> matches = EvaluateActionConstraintsCore(context, actionsWithConstraint, order);
                if (matches?.Count > 0)
                    return matches;
            }

            // If the set of matches with constraints can't work, then process the set without constraints.
            return actionsWithoutConstraint.Count == 0 ? null : EvaluateActionConstraintsCore(context, actionsWithoutConstraint, order);
        }
    }
}