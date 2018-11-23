using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using DotJEM.AspNetCore.FluentRouting.Invoker.Execution;
using DotJEM.AspNetCore.FluentRouting.Routing;
using DotJEM.AspNetCore.FluentRouting.Routing.Lambdas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Logging;

namespace DotJEM.AspNetCore.FluentRouting.Invoker
{
    public class LambdaInvoker : ResourceInvoker, IActionInvoker
    {
        //TODO:private readonly ControllerActionInvokerCacheEntry _cacheEntry;
        private readonly LambdaActionContext context;

        private readonly LambdaInvokerCacheEntry cacheEntry;

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
        internal LambdaInvoker(ILogger logger, DiagnosticSource diagnosticSource, LambdaActionContext context, LambdaInvokerCacheEntry cacheEntry, IFilterMetadata[] filters)
            : base(diagnosticSource, logger, new ActionResultTypeMapper(), context, filters, context.ValueProviderFactories)
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
                    _cursor.Reset();
                    // TODO: instance is not a Controller in our case, instead it's a Delegate.
                    // _instance = _cacheEntry.ControllerFactory(controllerContext);
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
                    Debug.Assert(actionExecutingContext != null);

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
                        _logger.LogDebug(-1, "_logger.ActionFilterShortCircuited(filter);");

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
                    Debug.Assert(actionExecutingContext != null);

                    IActionFilter filter = (IActionFilter)state;

                    _diagnosticSource.BeforeOnActionExecuting(actionExecutingContext, filter);
                    filter.OnActionExecuting(actionExecutingContext);
                    _diagnosticSource.AfterOnActionExecuting(actionExecutingContext, filter);

                    if (actionExecutingContext.Result != null)
                    {
                        // Short-circuited by setting a result.
                        _logger.LogDebug("_logger.ActionFilterShortCircuited(filter);");

                        actionExecutedContext = new ActionExecutedContext(actionExecutingContext, _filters, _instance)
                        {
                            Canceled = true,
                            Result = actionExecutingContext.Result,
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
                    Debug.Assert(actionExecutedContext != null);

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
                        if (actionExecutedContext == null)
                        {
                            actionExecutedContext = new ActionExecutedContext(context, _filters, _instance)
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
                throw new InvalidOperationException("Resources.FormatAsyncActionFilter_InvalidShortCircuit(" +
                                                    "typeof(IAsyncActionFilter).Name, " +
                                                    "nameof(ActionExecutingContext.Result), " +
                                                    "typeof(ActionExecutingContext).Name, " +
                                                    "typeof(ActionExecutionDelegate).Name);");
            }

            await InvokeNextActionFilterAsync();

            Debug.Assert(actionExecutedContext != null);
            return actionExecutedContext;
        }

        private async Task InvokeActionMethodAsync()
        {
            LambdaDescriptor fac = context.ActionDescriptor as LambdaDescriptor;
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
            Type resultType = executor.AsyncResultType ?? executor.ReturnType; //TODO: Suspicious
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
            return cacheEntry.LambdaBinderDelegate(context, arguments);
        }

        private object[] PrepareArguments(IDictionary<string, object> actionParameters, LambdaExecutor executor)
        {
            ParameterInfo[] parameters = executor.Parameters;
            int count = parameters.Length;
            if (count == 0)
                return null;
                        
            //IServiceProvider services = context.HttpContext.RequestServices;
            object[] arguments = new object[count];
            for (int i = 0; i < count; i++)
            {
                ParameterInfo parameterInfo = parameters[i];
                if (!actionParameters.TryGetValue(parameterInfo.Name, out object value))
                {
                    //TODO: Special case, but we should mobe this to the binder delegate!
                    if (parameterInfo.ParameterType == typeof(HttpContext))
                        value = context.HttpContext;
                }
                arguments[i] = value;
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