using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotJEM.AspNetCore.FluentRouter.Routing
{
    public class FluentRouteHandler : IRouter
    {
        private readonly IServiceProvider serviceProvider;

        private readonly ILogger logger;
        private readonly IActionContextAccessor actionContextAccessor;
        private readonly IActionInvokerFactory actionInvokerFactory;

        public FluentRouteHandler(
            ILoggerFactory loggerFactory, 
            IServiceProvider serviceProvider,
            IActionInvokerFactory actionInvokerFactory,
            IActionContextAccessor actionContextAccessor = null)
        {
            this.serviceProvider = serviceProvider;
            this.actionInvokerFactory = actionInvokerFactory;
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

            IFluentActionDescriptorFactory factory = serviceProvider.GetService<IFluentActionDescriptorFactory>();
            IFluentActionDescriptorCache cache = new FluentActionDescriptorCache(factory);

            FluentActionSelector selector = new FluentActionSelector(cache, serviceProvider.GetService<ActionConstraintCache>());

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
        


        //public MvcRouteHandler(IActionInvokerFactory actionInvokerFactory, IActionSelector actionSelector, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory)
        //    : this(actionInvokerFactory, actionSelector, diagnosticSource, loggerFactory, (IActionContextAccessor)null)
        //{
        //}

        //public MvcRouteHandler(IActionInvokerFactory actionInvokerFactory, IActionSelector actionSelector, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory, IActionContextAccessor actionContextAccessor)
        //{
        //    this._actionContextAccessor = actionContextAccessor;
        //    this._actionInvokerFactory = actionInvokerFactory;
        //    this._actionSelector = actionSelector;
        //    this._diagnosticSource = diagnosticSource;
        //    this._logger = (ILogger)loggerFactory.CreateLogger<MvcRouteHandler>();
        //}
    }
}