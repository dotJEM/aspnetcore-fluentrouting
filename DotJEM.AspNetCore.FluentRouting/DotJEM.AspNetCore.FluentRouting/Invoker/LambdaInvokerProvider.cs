using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DotJEM.AspNetCore.FluentRouting.Routing.Lambdas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotJEM.AspNetCore.FluentRouting.Invoker
{
    public class LambdaInvokerProvider : IActionInvokerProvider
    {
        //private readonly ControllerActionInvokerCache _controllerActionInvokerCache;
        //private readonly int _maxModelValidationErrors;

        private readonly ILogger logger;
        private readonly DiagnosticSource diagnosticSource;
        private readonly LambdaInvokerCache cache;
        private readonly IReadOnlyList<IValueProviderFactory> valueProviderFactories;

        public int Order => -1000;

        public LambdaInvokerProvider(
            //IControllerArgumentBinder argumentBinder, 
            LambdaInvokerCache cache,
            IOptions<MvcOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            DiagnosticSource diagnosticSource)
        {
            valueProviderFactories = optionsAccessor.Value.ValueProviderFactories.ToArray();
            //this._maxModelValidationErrors = optionsAccessor.Value.MaxModelValidationErrors;

            logger = loggerFactory.CreateLogger<LambdaInvoker>();
            this.cache = cache;
            this.diagnosticSource = diagnosticSource;
        }

        /// <inheritdoc />
        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!(context.ActionContext.ActionDescriptor is LambdaDescriptor))
                return;

            //ControllerContext controllerContext = new ControllerContext(context.ActionContext);
            //controllerContext.ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(this._valueProviderFactories);
            //controllerContext.ModelState.MaxAllowedErrors = this._maxModelValidationErrors;
            //ValueTuple<ControllerActionInvokerCacheEntry, IFilterMetadata[]> cachedResult = this._controllerActionInvokerCache.GetCachedResult(controllerContext);
            //ControllerActionInvoker controllerActionInvoker = new ControllerActionInvoker(
            //             this._logger, 
            //             this._diagnosticSource, 
            //             controllerContext, 
            //             cachedResult.Item1, 
            //             cachedResult.Item2);
            //context.Result = (IActionInvoker)controllerActionInvoker;
            LambdaActionContext ctx = new LambdaActionContext(context.ActionContext, new CopyOnWriteList<IValueProviderFactory>(valueProviderFactories));
            
            (LambdaInvokerCacheEntry entry, IFilterMetadata[] filters) = cache.Lookup(ctx);

            context.Result = new LambdaInvoker(logger, diagnosticSource, ctx, entry, filters);
        }


        /// <inheritdoc />
        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {
        }
    }
}