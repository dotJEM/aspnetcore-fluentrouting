using System;
using System.Runtime.CompilerServices;

namespace DotJEM.AspNetCore.FluentRouting.Invoker.MSInternal
{
    //NOTE: All this is more or less taken directly from https://github.com/aspnet/Common and same licence should be applied to these particular files.
    //      Reason for copy is that they where internal
    public struct LambdaExecutorAwaitable
    {
        private readonly object customAwaitable;
        private readonly Func<object, object> getAwaiterMethod;
        private readonly Func<object, bool> isCompletedMethod;
        private readonly Func<object, object> getResultMethod;
        private readonly Action<object, Action> onCompletedMethod;
        private readonly Action<object, Action> unsafeOnCompletedMethod;

        public LambdaExecutorAwaitable(object customAwaitable, Func<object, object> getAwaiterMethod, Func<object, bool> isCompletedMethod, Func<object, object> getResultMethod, Action<object, Action> onCompletedMethod, Action<object, Action> unsafeOnCompletedMethod)
        {
            this.customAwaitable = customAwaitable;
            this.getAwaiterMethod = getAwaiterMethod;
            this.isCompletedMethod = isCompletedMethod;
            this.getResultMethod = getResultMethod;
            this.onCompletedMethod = onCompletedMethod;
            this.unsafeOnCompletedMethod = unsafeOnCompletedMethod;
        }

        public Awaiter GetAwaiter() => new Awaiter(getAwaiterMethod(customAwaitable), isCompletedMethod, getResultMethod, onCompletedMethod, unsafeOnCompletedMethod);

        public struct Awaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private readonly object customAwaiter;
            private readonly Func<object, bool> isCompletedMethod;
            private readonly Func<object, object> getResultMethod;
            private readonly Action<object, Action> onCompletedMethod;
            private readonly Action<object, Action> unsafeOnCompletedMethod;
            public bool IsCompleted => isCompletedMethod(customAwaiter);

            public Awaiter(object customAwaiter, Func<object, bool> isCompletedMethod, Func<object, object> getResultMethod, Action<object, Action> onCompletedMethod, Action<object, Action> unsafeOnCompletedMethod)
            {
                this.customAwaiter = customAwaiter;
                this.isCompletedMethod = isCompletedMethod;
                this.getResultMethod = getResultMethod;
                this.onCompletedMethod = onCompletedMethod;
                this.unsafeOnCompletedMethod = unsafeOnCompletedMethod;
            }

            public object GetResult() => getResultMethod(customAwaiter);
            public void OnCompleted(Action continuation) => onCompletedMethod(customAwaiter, continuation);
            public void UnsafeOnCompleted(Action continuation) => (unsafeOnCompletedMethod ?? onCompletedMethod)(customAwaiter, continuation);
        }
    }
}