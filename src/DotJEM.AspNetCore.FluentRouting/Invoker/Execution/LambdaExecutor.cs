using System;
using System.Reflection;
using DotJEM.AspNetCore.FluentRouting.Invoker.MSInternal;
using DotJEM.AspNetCore.FluentRouting.Routing.Lambdas;

namespace DotJEM.AspNetCore.FluentRouting.Invoker.Execution
{
    public abstract class LambdaExecutor
    {
        public Delegate Target { get; }
        public Type ReturnType { get; }
        public ParameterInfo[] Parameters { get; }
        public bool IsMethodAsync { get; }
        public Type AsyncResultType { get; set; }

        protected LambdaExecutor(Delegate target, Type asyncResultType = null)
        {
            Target = target;
            Parameters = target.Method.GetParameters();
            ReturnType = target.Method.ReturnType;

            IsMethodAsync = asyncResultType != null;
            AsyncResultType = asyncResultType;
        }

        public abstract object Execute(object[] arguments);

        public abstract LambdaExecutorAwaitable ExecuteAsync(object[] arguments);
    }

    public class AsyncLambdaExecutor : LambdaExecutor
    {
        private readonly ActionExecutorDelegate executorDelegate;
        private readonly AsyncActionExecutorDelegate asyncExecutorDelegate;

        public AsyncLambdaExecutor(
            Delegate target,
            ActionExecutorDelegate executorDelegate,
            AsyncActionExecutorDelegate asyncExecutorDelegate,
            Type asyncResultType)
            : base(target, asyncResultType)
        {
            this.executorDelegate = executorDelegate;
            this.asyncExecutorDelegate = asyncExecutorDelegate;
        }

        public override object Execute(object[] arguments) => executorDelegate(Target, arguments);
        public override LambdaExecutorAwaitable ExecuteAsync(object[] arguments) => asyncExecutorDelegate(Target, arguments);
    }

    public class SyncLambdaExecutor : LambdaExecutor
    {
        private readonly ActionExecutorDelegate executorDelegate;

        public SyncLambdaExecutor(Delegate target, ActionExecutorDelegate executorDelegate)
            : base(target)
        {
            this.executorDelegate = executorDelegate;
        }

        public override object Execute(object[] arguments) => executorDelegate(Target, arguments);

        public override LambdaExecutorAwaitable ExecuteAsync(object[] arguments) => throw new NotSupportedException("");
    }
}