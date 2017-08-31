using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using DotJEM.AspNetCore.FluentRouter.Invoker.MSInternal;
using DotJEM.AspNetCore.FluentRouter.Routing;
using Microsoft.AspNetCore.Mvc;

namespace DotJEM.AspNetCore.FluentRouter.Invoker
{
    public delegate void VoidActionExecutorDelegate(Delegate target, object[] parameters);
    public delegate object ActionExecutorDelegate(Delegate target, object[] parameters);
    public delegate LambdaExecutorAwaitable AsyncActionExecutorDelegate(Delegate target, object[] parameters);

    public interface ILambdaExecutorDelegateFactory
    {
        ActionExecutorDelegate Create(Delegate target);
        AsyncActionExecutorDelegate CreateAsync(Delegate target, CoercedAwaitableInfo coercedAwaitableInfo);
    }

    public class LambdaExecutorDelegateFactory : ILambdaExecutorDelegateFactory
    {
        public ActionExecutorDelegate Create(Delegate target)
        {
            // Parameters for the delegate:
            ParameterExpression targetParameter = Expression.Parameter(typeof(Delegate), "target");
            ParameterExpression parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            MethodCallExpression methodCall = CreateMethodCall(target, targetParameter, parametersParameter);

            // Create function
            if (methodCall.Type == typeof(void))
            {
                //TODO: Void support for Action<T, ..> although we might be able to wrap that in a Func<Task> (async)
                //var lambda = Expression.Lambda<VoidMethodExecutor>(methodCall, targetParameter, parametersParameter);
                //var voidExecutor = lambda.Compile();
                //return WrapVoidMethod(voidExecutor);
                return null;
            }

            // Create function
            // must coerce methodCall to match ActionExecutorDelegate signature
            UnaryExpression castMethodCall = Expression.Convert(methodCall, typeof(object));
            Expression<ActionExecutorDelegate> lambda = Expression.Lambda<ActionExecutorDelegate>(castMethodCall, targetParameter, parametersParameter);
            return lambda.Compile();
        }

        public AsyncActionExecutorDelegate CreateAsync(Delegate target, CoercedAwaitableInfo coercedAwaitableInfo)
        {
            // Parameters for the delegate:
            ParameterExpression targetParameter = Expression.Parameter(typeof(Delegate), "target");
            ParameterExpression parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            MethodCallExpression methodCall = CreateMethodCall(target, targetParameter, parametersParameter);
            // Using the method return value, construct an ObjectMethodExecutorAwaitable based on
            // the info we have about its implementation of the awaitable pattern. Note that all
            // the funcs/actions we construct here are precompiled, so that only one instance of
            // each is preserved throughout the lifetime of the ObjectMethodExecutor.

            // var getAwaiterFunc = (object awaitable) => (object)((CustomAwaitableType)awaitable).GetAwaiter();
            ParameterExpression customAwaitableParam = Expression.Parameter(typeof(object), "awaitable");
            AwaitableInfo awaitableInfo = coercedAwaitableInfo.AwaitableInfo;

            Type postCoercionMethodReturnType = coercedAwaitableInfo.CoercerResultType ?? target.Method.ReturnType;
            Func<object, object> getAwaiterFunc = Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Call(
                        Expression.Convert(customAwaitableParam, postCoercionMethodReturnType),
                        awaitableInfo.GetAwaiterMethod),
                    typeof(object)),
                customAwaitableParam).Compile();

            // var isCompletedFunc = (object awaiter) => ((CustomAwaiterType)awaiter).IsCompleted;
            ParameterExpression isCompletedParam = Expression.Parameter(typeof(object), "awaiter");
            Func<object, bool> isCompletedFunc = Expression.Lambda<Func<object, bool>>(
                Expression.MakeMemberAccess(
                    Expression.Convert(isCompletedParam, awaitableInfo.AwaiterType),
                    awaitableInfo.AwaiterIsCompletedProperty),
                isCompletedParam).Compile();

            ParameterExpression getResultParam = Expression.Parameter(typeof(object), "awaiter");
            Func<object, object> getResultFunc;
            if (awaitableInfo.ResultType == typeof(void))
            {
                // var getResultFunc = (object awaiter) =>  {
                //     ((CustomAwaiterType)awaiter).GetResult(); // We need to invoke this to surface any exceptions
                //     return (object)null;
                // };
                getResultFunc = Expression.Lambda<Func<object, object>>(
                    Expression.Block(
                        Expression.Call(
                            Expression.Convert(getResultParam, awaitableInfo.AwaiterType),
                            awaitableInfo.AwaiterGetResultMethod),
                        Expression.Constant(null)
                    ),
                    getResultParam).Compile();
            }
            else
            {
                // var getResultFunc = (object awaiter) => (object)((CustomAwaiterType)awaiter).GetResult();
                getResultFunc = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Call(
                            Expression.Convert(getResultParam, awaitableInfo.AwaiterType),
                            awaitableInfo.AwaiterGetResultMethod),
                        typeof(object)),
                    getResultParam).Compile();
            }

            // var onCompletedFunc = (object awaiter, Action continuation) => {
            //     ((CustomAwaiterType)awaiter).OnCompleted(continuation);
            // };
            ParameterExpression onCompletedParam1 = Expression.Parameter(typeof(object), "awaiter");
            ParameterExpression onCompletedParam2 = Expression.Parameter(typeof(Action), "continuation");
            Action<object, Action> onCompletedFunc = Expression.Lambda<Action<object, Action>>(
                Expression.Call(
                    Expression.Convert(onCompletedParam1, awaitableInfo.AwaiterType),
                    awaitableInfo.AwaiterOnCompletedMethod,
                    onCompletedParam2),
                onCompletedParam1,
                onCompletedParam2).Compile();

            Action<object, Action> unsafeOnCompletedFunc = null;
            if (awaitableInfo.AwaiterUnsafeOnCompletedMethod != null)
            {
                // var unsafeOnCompletedFunc = (object awaiter, Action continuation) => {
                //     ((CustomAwaiterType)awaiter).UnsafeOnCompleted(continuation);
                // };
                ParameterExpression unsafeOnCompletedParam1 = Expression.Parameter(typeof(object), "awaiter");
                ParameterExpression unsafeOnCompletedParam2 = Expression.Parameter(typeof(Action), "continuation");
                unsafeOnCompletedFunc = Expression.Lambda<Action<object, Action>>(
                    Expression.Call(
                        Expression.Convert(unsafeOnCompletedParam1, awaitableInfo.AwaiterType),
                        awaitableInfo.AwaiterUnsafeOnCompletedMethod,
                        unsafeOnCompletedParam2),
                    unsafeOnCompletedParam1,
                    unsafeOnCompletedParam2).Compile();
            }

            // If we need to pass the method call result through a coercer function to get an
            // awaitable, then do so.
            Expression coercedMethodCall = coercedAwaitableInfo.RequiresCoercion
                ? Expression.Invoke(coercedAwaitableInfo.CoercerExpression, methodCall)
                : (Expression)methodCall;

            // return new ObjectMethodExecutorAwaitable(
            //     (object)coercedMethodCall,
            //     getAwaiterFunc,
            //     isCompletedFunc,
            //     getResultFunc,
            //     onCompletedFunc,
            //     unsafeOnCompletedFunc);
            NewExpression returnValueExpression = Expression.New(
                lambdaExecutorAwaitableConstructor,
                Expression.Convert(coercedMethodCall, typeof(object)),
                Expression.Constant(getAwaiterFunc),
                Expression.Constant(isCompletedFunc),
                Expression.Constant(getResultFunc),
                Expression.Constant(onCompletedFunc),
                Expression.Constant(unsafeOnCompletedFunc, typeof(Action<object, Action>)));

            Expression<AsyncActionExecutorDelegate> lambda = Expression.Lambda<AsyncActionExecutorDelegate>(returnValueExpression, targetParameter, parametersParameter);
            return lambda.Compile();
        }

        private static readonly ConstructorInfo lambdaExecutorAwaitableConstructor =
            typeof(LambdaExecutorAwaitable).GetConstructor(new[] {
                typeof(object),                 // customAwaitable
                typeof(Func<object, object>),   // getAwaiterMethod
                typeof(Func<object, bool>),     // isCompletedMethod
                typeof(Func<object, object>),   // getResultMethod
                typeof(Action<object, Action>), // onCompletedMethod
                typeof(Action<object, Action>)  // unsafeOnCompletedMethod
            });

        private UnaryExpression CreateParameterCast(BinaryExpression accessor, Type type)
        {
            if (type.IsGenericType)
            {
                Type firstInnerType = type.GenericTypeArguments.First();
                Type bindingSourceParameterType = typeof(BindingSourceParameter<>).MakeGenericType(firstInnerType);
                if (bindingSourceParameterType.IsAssignableFrom(type))
                {
                    // castParameter: "(Ti) (FromBody<T..>) parameters[i]"
                    return  Expression.Convert(Expression.Convert(accessor, firstInnerType), type);
                }
            }
            // castParameter: "(Ti) parameters[i]"
            return Expression.Convert(accessor, type);
        }

        private MethodCallExpression CreateMethodCall(Delegate target, ParameterExpression targetParameter, ParameterExpression parametersParameter)
        {
            List<Expression> parameters = BuildParameterList(target.Method, parametersParameter);

            Type delegateType = target.GetType();
            UnaryExpression instanceCast = Expression.Convert(targetParameter, delegateType);
            // methodCall: ((Func<...>/Action<...>) target) @delegate.Invoke((T0) parameters[0], (T1) parameters[1], ...)
            return Expression.Call(instanceCast, delegateType.GetMethod("Invoke"), parameters);
        }

        private List<Expression> BuildParameterList(MethodInfo method, ParameterExpression source)
        {
            List<Expression> parameters = new List<Expression>();
            ParameterInfo[] infos = method.GetParameters();
            for (int i = 0; i < infos.Length; i++)
            {
                ParameterInfo info = infos[i];
                // arrayIndexAccessor: parameters[i]
                BinaryExpression arrayIndexAccessor = Expression.ArrayIndex(source, Expression.Constant(i));
                // castParameter: "(Ti) (FromBody<T..>) parameters[i]" or "(Ti) parameters[i]".
                UnaryExpression castParameter = CreateParameterCast(arrayIndexAccessor, info.ParameterType);
                parameters.Add(castParameter);
            }
            return parameters;
        }
    }

    public class LambdaExecutor
    {
        private readonly Delegate target;
        private readonly ActionExecutorDelegate executorDelegate;
        private readonly AsyncActionExecutorDelegate asyncExecutorDelegate;

        public Type ReturnType { get; }
        public ParameterInfo[] Parameters { get; }
        public bool IsMethodAsync { get; }
        public Type AsyncResultType { get; set; }

        public LambdaExecutor(LambdaActionDescriptor descriptor, ActionExecutorDelegate executorDelegate)
        {
            this.executorDelegate = executorDelegate;

            target = descriptor.Delegate;

            Parameters = descriptor.Delegate.Method.GetParameters();
            ReturnType = descriptor.Delegate.Method.ReturnType;

            //TODO: Awaitable?
            IsMethodAsync = false;
            AsyncResultType = null;
        }

        public LambdaExecutor(
            LambdaActionDescriptor descriptor,
            ActionExecutorDelegate executorDelegate, 
            AsyncActionExecutorDelegate asyncExecutorDelegate, Type asyncResultType)
        {
            this.executorDelegate = executorDelegate;
            this.asyncExecutorDelegate = asyncExecutorDelegate;

            target = descriptor.Delegate;

            Parameters = descriptor.Delegate.Method.GetParameters();
            ReturnType = descriptor.Delegate.Method.ReturnType;

            IsMethodAsync = true;
            AsyncResultType = asyncResultType;
        }

        public object Execute(object[] arguments) => executorDelegate(target, arguments);

        public LambdaExecutorAwaitable ExecuteAsync(object[] arguments) => asyncExecutorDelegate(target, arguments);
    }
}