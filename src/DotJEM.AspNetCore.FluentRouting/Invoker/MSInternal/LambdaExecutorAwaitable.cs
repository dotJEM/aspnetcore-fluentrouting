using System;
using System.Runtime.CompilerServices;

namespace DotJEM.AspNetCore.FluentRouting.Invoker.MSInternal
{

    /* This class originates from the https://github.com/aspnet/Common project, as it is
     * internal a slightly modified version of the code has been copied into this source,
     * the license of the original source is listed below.
     * --------------------------------------------------------------------------------------
     *
     * Copyright (c) .NET Foundation and Contributors
     *
     * All rights reserved.
     *
     * Licensed under the Apache License, Version 2.0 (the "License"); you may not use
     * this file except in compliance with the License. You may obtain a copy of the
     * License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software distributed
     * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
     * CONDITIONS OF ANY KIND, either express or implied. See the License for the
     * specific language governing permissions and limitations under the License.
     */

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