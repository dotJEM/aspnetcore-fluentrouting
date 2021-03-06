using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotJEM.AspNetCore.FluentRouting.Invoker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotJEM.AspNetCore.FluentRouting.Routing
{
    public class FluentRouteHandler : IRouter
    {
        private readonly ILogger logger;
        private readonly IActionContextAccessor actionContextAccessor;
        private readonly IActionInvokerFactory actionInvokerFactory;
        private readonly IActionSelector selector;

        public FluentRouteHandler(
            ILoggerFactory loggerFactory, 
            IActionInvokerFactory actionInvokerFactory, 
            IActionSelector selector, 
            IActionContextAccessor actionContextAccessor = null)
        {
            this.actionInvokerFactory = actionInvokerFactory;
            this.selector = selector;
            this.actionContextAccessor = actionContextAccessor;
            this.logger = loggerFactory.CreateLogger<FluentRouteHandler>();
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            
            IReadOnlyList<ActionDescriptor> candidates = selector.SelectCandidates(context);
            if (candidates == null || candidates.Count < 1)
            {
                logger.LogDebug(-1, "No actions matched the current request. Route values: {RouteValues}");
                return Task.CompletedTask;
            }
            ActionDescriptor actionDescriptor = selector.SelectBestCandidate(context, candidates);
            if (actionDescriptor == null)
            {
                logger.LogDebug(-1, "No actions matched the current request. Route values: {RouteValues}");
                return Task.CompletedTask;
            }

            context.Handler = c =>
            {
                ActionContext actionContext = new ActionContext(context.HttpContext, c.GetRouteData(), actionDescriptor);
                if (actionContextAccessor != null)
                    actionContextAccessor.ActionContext = actionContext;
                IActionInvoker invoker = actionInvokerFactory.CreateInvoker(actionContext);
                if (invoker == null)
                    throw new InvalidOperationException("Resources.FormatActionInvokerFactory_CouldNotCreateInvoker(actionDescriptor.DisplayName)");
                return invoker.InvokeAsync();
            };
            return Task.CompletedTask;
        }
    }
}