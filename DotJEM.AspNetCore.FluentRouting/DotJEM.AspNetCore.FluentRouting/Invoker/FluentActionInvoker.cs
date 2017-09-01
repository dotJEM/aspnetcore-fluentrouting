using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using DotJEM.AspNetCore.FluentRouter.Invoker;
using DotJEM.AspNetCore.FluentRouter.Invoker.MSInternal;
using DotJEM.AspNetCore.FluentRouter.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotJEM.AspNetCore.FluentRouter
{
    public class FluentActionInvokerCache
    {
        private readonly ParameterBinder parameterBinder;
        private readonly IModelBinderFactory modelBinderFactory;
        private readonly IModelMetadataProvider modelMetadataProvider;
        private readonly IFilterProvider[] filterProviders;
        private readonly ILambdaExecutorDelegateFactory delegateFactory = new LambdaExecutorDelegateFactory();

        public FluentActionInvokerCache(
            ParameterBinder parameterBinder, 
            IModelBinderFactory modelBinderFactory,
            IModelMetadataProvider modelMetadataProvider,
            IEnumerable<IFilterProvider> filterProviders)
        {
            this.parameterBinder = parameterBinder;
            this.modelBinderFactory = modelBinderFactory;
            this.modelMetadataProvider = modelMetadataProvider;
            this.filterProviders = filterProviders.OrderBy(item => item.Order).ToArray();
        }

        public (FluentActionInvokerCacheEntry, IFilterMetadata[]) Lookup(FluentActionContext context)
        {
            //TODO: Cache

            LambdaActionDescriptor descriptor = context.ActionDescriptor as LambdaActionDescriptor;
            if (descriptor == null)
                return (null, null);

            IFilterMetadata[] filterMetadatas;
            FilterFactoryResult filterFactoryResult = FilterFactory.GetAllFilters(filterProviders, context);
            filterMetadatas = filterFactoryResult.Filters;

            //NOTE: Does not make sense for Func<T> ??
            //object[] parameterDefaultValues = ParameterDefaultValues.GetParameterDefaultValues(descriptor.Delegate.Method);
            CoercedAwaitableInfo awaitableInfo;

            LambdaExecutor executor;
            if (CoercedAwaitableInfo.TryGetAwaitableInfo(descriptor.Delegate.Method.ReturnType, out awaitableInfo))
            {
                executor = new LambdaExecutor(descriptor, delegateFactory.Create(descriptor.Delegate), delegateFactory.CreateAsync(descriptor.Delegate, awaitableInfo), awaitableInfo.AwaitableInfo.ResultType);
            }
            else
            {
                executor = new LambdaExecutor(descriptor, delegateFactory.Create(descriptor.Delegate));
            }

            //TODO: Func Binder Delegate Provider
            FunctionBinderDelegate functionBinder = FuncBinderDelegateProvider.CreateBinderDelegate(parameterBinder, modelBinderFactory, modelMetadataProvider, descriptor);

            FluentActionInvokerCacheEntry entry = new FluentActionInvokerCacheEntry(functionBinder, executor, filterFactoryResult.CacheableFilters);

            return (entry, filterMetadatas);
        }
    }

    public class FluentActionInvokerCacheEntry
    {
        public FunctionBinderDelegate FunctionBinderDelegate { get; }
        public FilterItem[] CacheableFilters { get; }
        public LambdaExecutor ActionExecutor { get; }

        public FluentActionInvokerCacheEntry(FunctionBinderDelegate functionBinder, LambdaExecutor executor, FilterItem[] cacheableFilters)
        {
            FunctionBinderDelegate = functionBinder;
            CacheableFilters = cacheableFilters;
            ActionExecutor = executor;
        }
    }

    public class FluentActionInvokerProvider : IActionInvokerProvider
    {
        //private readonly ControllerActionInvokerCache _controllerActionInvokerCache;
        //private readonly int _maxModelValidationErrors;
        //private readonly ILogger _logger;
        //private readonly DiagnosticSource _diagnosticSource;

        private readonly ILogger logger;
        private readonly DiagnosticSource diagnosticSource;
        private readonly FluentActionInvokerCache cache;
        private readonly IReadOnlyList<IValueProviderFactory> valueProviderFactories;

        public int Order => -1000;

        public FluentActionInvokerProvider(
            //IControllerFactory controllerFactory, 
            //ControllerActionInvokerCache controllerActionInvokerCache, 
            //IControllerArgumentBinder argumentBinder, 
            FluentActionInvokerCache cache,
            IOptions<MvcOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            DiagnosticSource diagnosticSource)
        {
            //this._controllerActionInvokerCache = controllerActionInvokerCache;
            this.valueProviderFactories = optionsAccessor.Value.ValueProviderFactories.ToArray();
            //this._maxModelValidationErrors = optionsAccessor.Value.MaxModelValidationErrors;
            //this._logger = (ILogger)loggerFactory.CreateLogger<ControllerActionInvoker>();
            //this._diagnosticSource = diagnosticSource;

            this.logger = loggerFactory.CreateLogger<FluentActionInvoker>();
            this.cache = cache;
            this.diagnosticSource = diagnosticSource;
        }

        /// <inheritdoc />
        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!(context.ActionContext.ActionDescriptor is LambdaActionDescriptor))
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
            FluentActionContext ctx = context.ActionContext as FluentActionContext;
            ctx.ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(valueProviderFactories);

            (FluentActionInvokerCacheEntry entry, IFilterMetadata[] filters) = cache.Lookup(ctx);

            context.Result = new FluentActionInvoker(logger, diagnosticSource, ctx, entry, filters);
        }


        /// <inheritdoc />
        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {
        }
    }

    public class FluentActionContext : ActionContext
    {




        public FluentActionContext(HttpContext context, RouteData data, ActionDescriptor descriptor)
            : base(context, data, descriptor)
        {
        }

        public IList<IValueProviderFactory> ValueProviderFactories { get; set; }
    }

    public class FluentActionInvoker : ResourceInvoker, IActionInvoker
    {
        //TODO:private readonly ControllerActionInvokerCacheEntry _cacheEntry;
        private readonly FluentActionContext context;

        private readonly FluentActionInvokerCacheEntry cacheEntry;

        private Dictionary<string, object> arguments;

        private ActionExecutingContext actionExecutingContext;
        private ActionExecutedContext actionExecutedContext;
        /*
         * (
         * ControllerActionInvokerCache cache, 
         * IControllerFactory controllerFactory, 
         * IControllerArgumentBinder controllerArgumentBinder, 
         * ILogger logger, 
         * DiagnosticSource diagnosticSource, 
         * ActionContext actionContext, 
         * IReadOnlyList<IValueProviderFactory> valueProviderFactories, 
         * int maxModelValidationErrors)
        */
        internal FluentActionInvoker(//IServiceProvider serviceProvider,
            //TODO:ControllerActionInvokerCacheEntry cacheEntry,
            ILogger logger, DiagnosticSource diagnosticSource, FluentActionContext context, FluentActionInvokerCacheEntry cacheEntry, IFilterMetadata[] filters)
            : base(diagnosticSource, logger, context, filters, context.ValueProviderFactories)
        {
            this.context = context;
            this.cacheEntry = cacheEntry ?? throw new ArgumentNullException(nameof(cacheEntry));
        }
        
        protected override void ReleaseResources()
        {
            //Note: There is no resources to release for functions.
        }

        private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
        {
            switch (next)
            {
                case State.ActionBegin:
                {
                    //var controllerContext = _controllerContext;

                    _cursor.Reset();

                    //TODO: _instance = _cacheEntry.ControllerFactory(controllerContext);

                    arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                    Task task = BindArgumentsAsync();
                    if (task.Status != TaskStatus.RanToCompletion)
                    {
                        next = State.ActionNext;
                        return task;
                    }

                    goto case State.ActionNext;
                }

                case State.ActionNext:
                {
                    FilterCursorItem<IActionFilter, IAsyncActionFilter> current = _cursor.GetNextFilter<IActionFilter, IAsyncActionFilter>();
                    if (current.FilterAsync != null)
                    {
                        if (actionExecutingContext == null)
                        {
                            actionExecutingContext = new ActionExecutingContext(context, _filters, arguments, _instance);
                        }

                        state = current.FilterAsync;
                        goto case State.ActionAsyncBegin;
                    }
                    if (current.Filter != null)
                    {
                        if (actionExecutingContext == null)
                        {
                            actionExecutingContext = new ActionExecutingContext(context, _filters, arguments, _instance);
                        }

                        state = current.Filter;
                        goto case State.ActionSyncBegin;
                    }
                    goto case State.ActionInside;
                }

                case State.ActionAsyncBegin:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(this.actionExecutingContext != null);

                    IAsyncActionFilter filter = (IAsyncActionFilter)state;

                    _diagnosticSource.BeforeOnActionExecution(actionExecutingContext, filter);

                    Task task = filter.OnActionExecutionAsync(actionExecutingContext, InvokeNextActionFilterAwaitedAsync);
                    if (task.Status != TaskStatus.RanToCompletion)
                    {
                        next = State.ActionAsyncEnd;
                        return task;
                    }

                    goto case State.ActionAsyncEnd;
                }

                case State.ActionAsyncEnd:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(actionExecutingContext != null);

                    IAsyncActionFilter filter = (IAsyncActionFilter)state;

                    if (actionExecutedContext == null)
                    {
                        // If we get here then the filter didn't call 'next' indicating a short circuit.
                        //TODO:_logger.ActionFilterShortCircuited(filter);

                        actionExecutedContext = new ActionExecutedContext(context,_filters,_instance)
                        {
                            Canceled = true,
                            Result = actionExecutingContext.Result,
                        };
                    }

                    _diagnosticSource.AfterOnActionExecution(actionExecutedContext, filter);

                    goto case State.ActionEnd;
                }

                case State.ActionSyncBegin:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(this.actionExecutingContext != null);

                    IActionFilter filter = (IActionFilter)state;
                    ActionExecutingContext actionExecutingContext = this.actionExecutingContext;

                    _diagnosticSource.BeforeOnActionExecuting(actionExecutingContext, filter);

                    filter.OnActionExecuting(actionExecutingContext);

                    _diagnosticSource.AfterOnActionExecuting(actionExecutingContext, filter);

                    if (actionExecutingContext.Result != null)
                    {
                        // Short-circuited by setting a result.
                        //TODO:_logger.ActionFilterShortCircuited(filter);

                        actionExecutedContext = new ActionExecutedContext(
                            this.actionExecutingContext,
                            _filters,
                            _instance)
                        {
                            Canceled = true,
                            Result = this.actionExecutingContext.Result,
                        };

                        goto case State.ActionEnd;
                    }

                    Task task = InvokeNextActionFilterAsync();
                    if (task.Status != TaskStatus.RanToCompletion)
                    {
                        next = State.ActionSyncEnd;
                        return task;
                    }

                    goto case State.ActionSyncEnd;
                }

                case State.ActionSyncEnd:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(actionExecutingContext != null);
                    Debug.Assert(this.actionExecutedContext != null);

                    IActionFilter filter = (IActionFilter)state;

                    _diagnosticSource.BeforeOnActionExecuted(actionExecutedContext, filter);

                    filter.OnActionExecuted(actionExecutedContext);

                    _diagnosticSource.AfterOnActionExecuted(actionExecutedContext, filter);

                    goto case State.ActionEnd;
                }

                case State.ActionInside:
                {
                    Task task = InvokeActionMethodAsync();
                    if (task.Status != TaskStatus.RanToCompletion)
                    {
                        next = State.ActionEnd;
                        return task;
                    }

                    goto case State.ActionEnd;
                }

                case State.ActionEnd:
                {
                    if (scope == Scope.Action)
                    {
                        if (this.actionExecutedContext == null)
                        {
                            this.actionExecutedContext = new ActionExecutedContext(context, _filters, _instance)
                            {
                                Result = _result,
                            };
                        }

                        isCompleted = true;
                        return Task.CompletedTask;
                    }

                    Rethrow(actionExecutedContext);

                    if (actionExecutedContext != null)
                    {
                        _result = actionExecutedContext.Result;
                    }

                    isCompleted = true;
                    return Task.CompletedTask;
                }

                default:
                    throw new InvalidOperationException();
            }
        }

        private async Task InvokeNextActionFilterAsync()
        {
            try
            {
                State next = State.ActionNext;
                object state = (object)null;
                Scope scope = Scope.Action;
                bool isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                actionExecutedContext = new ActionExecutedContext(context, _filters, _instance)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(actionExecutedContext != null);
        }

        private async Task<ActionExecutedContext> InvokeNextActionFilterAwaitedAsync()
        {
            Debug.Assert(actionExecutingContext != null);
            if (actionExecutingContext.Result != null)
            {
                // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
                string message = "Resources.FormatAsyncActionFilter_InvalidShortCircuit("; //TODO
                //typeof(IAsyncActionFilter).Name,
                //nameof(ActionExecutingContext.Result),
                //typeof(ActionExecutingContext).Name,
                //typeof(ActionExecutionDelegate).Name);

                throw new InvalidOperationException(message);
            }

            await InvokeNextActionFilterAsync();

            Debug.Assert(actionExecutedContext != null);
            return actionExecutedContext;
        }

        private async Task InvokeActionMethodAsync()
        {
            LambdaActionDescriptor fac = context.ActionDescriptor as LambdaActionDescriptor;
            if (fac == null)
                throw new InvalidOperationException();

            LambdaExecutor executor = cacheEntry.ActionExecutor;

            object[] args = PrepareArguments(arguments, executor);

            DiagnosticSource diagnosticSource = _diagnosticSource;
            ILogger logger = _logger;

            IActionResult result = null;
            try
            {
                diagnosticSource.BeforeActionMethod(context, arguments, null);
                logger.LogDebug(-1, "ActionMethodExecuting(controllerContext, orderedArguments)");

                Type returnType = executor.ReturnType;
                if (returnType == typeof(void))
                {
                    // Sync method returning void
                    executor.Execute(args);
                    result = new EmptyResult();
                }
                else if (returnType == typeof(Task))
                {
                    // Async method returning Task
                    // Avoid extra allocations by calling Execute rather than ExecuteAsync and casting to Task.
                    await (Task)executor.Execute(args);
                    result = new EmptyResult();
                }
                else if (returnType == typeof(Task<IActionResult>))
                {
                    // Async method returning Task<IActionResult>
                    // Avoid extra allocations by calling Execute rather than ExecuteAsync and casting to Task<IActionResult>.
                    result = await (Task<IActionResult>)executor.Execute(args);
                    if (result == null)
                    {
                        throw new InvalidOperationException("Resources.FormatActionResult_ActionReturnValueCannotBeNull(typeof(IActionResult))");
                    }
                }
                else if (IsResultIActionResult(executor))
                {
                    if (executor.IsMethodAsync)
                    {
                        // Async method returning awaitable-of-IActionResult (e.g., Task<ViewResult>)
                        // We have to use ExecuteAsync because we don't know the awaitable's type at compile time.
                        result = (IActionResult)await executor.ExecuteAsync(args);
                    }
                    else
                    {
                        // Sync method returning IActionResult (e.g., ViewResult)
                        result = (IActionResult)executor.Execute(args);
                    }

                    if (result == null)
                    {
                        throw new InvalidOperationException("Resources.FormatActionResult_ActionReturnValueCannotBeNull(executor.AsyncResultType ?? returnType)");
                    }
                }
                else if (!executor.IsMethodAsync)
                {
                    // Sync method returning arbitrary object
                    object resultAsObject = executor.Execute(args);
                    result = resultAsObject as IActionResult ?? new ObjectResult(resultAsObject)
                    {
                        DeclaredType = returnType,
                    };
                }
                else if (executor.AsyncResultType == typeof(void))
                {
                    // Async method returning awaitable-of-void
                    await executor.ExecuteAsync(args);
                    result = new EmptyResult();
                }
                else
                {
                    // Async method returning awaitable-of-nonvoid
                    object resultAsObject = await executor.ExecuteAsync(args);
                    result = resultAsObject as IActionResult ?? new ObjectResult(resultAsObject)
                    {
                        DeclaredType = executor.AsyncResultType
                    };
                }

                _result = result;
                logger.LogDebug(-1, "logger.ActionMethodExecuted(controllerContext, result);");
            }
            finally
            {
                diagnosticSource.AfterActionMethod(context,
                    arguments,
                    context,
                    result);
            }
            
        }
        
        private static bool IsResultIActionResult(LambdaExecutor executor)
        {
            Type resultType = /*executor.AsyncResultType ?? */executor.ReturnType;
            return typeof(IActionResult).IsAssignableFrom(resultType);
        }

        /// <remarks><see cref="ResourceInvoker.InvokeFilterPipelineAsync"/> for details on what the
        /// variables in this method represent.</remarks>
        protected override async Task InvokeInnerFilterAsync()
        {
            State next = State.ActionBegin;
            Scope scope = Scope.Invoker;
            object state = null;
            bool isCompleted = false;
            
            while (!isCompleted)
            {
                await Next(ref next, ref scope, ref state, ref isCompleted);
            }
        }

        private static void Rethrow(ActionExecutedContext context)
        {
            if (context == null || context.ExceptionHandled)
                return;

            context.ExceptionDispatchInfo?.Throw();
            if (context.Exception != null)
                throw context.Exception;
        }

        private Task BindArgumentsAsync()
        {
            // Perf: Avoid allocating async state machines where possible. We only need the state
            // machine if you need to bind properties or arguments.
            ActionDescriptor actionDescriptor = context.ActionDescriptor;
            if (actionDescriptor.Parameters.Count == 0)
            {
                return Task.CompletedTask;
            }
            return cacheEntry.FunctionBinderDelegate(context, arguments);
        }

        private object[] PrepareArguments(IDictionary<string, object> actionParameters, LambdaExecutor executor)
        {
            ParameterInfo[] parameters = executor.Parameters; //TODO: actionMethodExecutor.MethodParameters;
            int count = parameters.Length;
            if (count == 0)
                return null;
                        
            IServiceProvider services = context.HttpContext.RequestServices;
            object[] arguments = new object[count];
            for (int i = 0; i < count; i++)
            {
                ParameterInfo parameterInfo = parameters[i];
                if (!actionParameters.TryGetValue(parameterInfo.Name, out object value))
                {
                    if (parameterInfo.ParameterType == typeof(HttpContext))
                        value = context.HttpContext;
                }

                arguments[i] = value;

                if (parameterInfo.ParameterType.IsGenericType)
                {
                    Type gt = parameterInfo.ParameterType.GetGenericTypeDefinition();
                    if (gt == typeof(FromHeader<>))
                    {
                    }
                    if (gt == typeof(FromServices<>))
                    {
                    }
                    if (gt == typeof(FromRoute<>))
                    {
                        //TODO: This won't work for more complex types, we need a way to say that 
                        //      AspNetCore should run all it's useual fomartters/binders and then just finally
                        //      wrap that up into a FromRoute<T> value.
                        //arguments[i] = Activator.CreateInstance(parameterInfo.ParameterType, value);
                    }
                    if (gt == typeof(FromQuery<>))
                    {
                    }
                    if (gt == typeof(FromForm<>))
                    {
                    }
                    if (gt == typeof(FromBody<>))
                    {
                    }
                    if (gt == typeof(FromUri<>))
                    {
                    }
                }

            }

            return arguments;
        }

        private enum Scope
        {
            Invoker,
            Action,
        }

        private enum State
        {
            ActionBegin,
            ActionNext,
            ActionAsyncBegin,
            ActionAsyncEnd,
            ActionSyncBegin,
            ActionSyncEnd,
            ActionInside,
            ActionEnd,
        }
    }
}