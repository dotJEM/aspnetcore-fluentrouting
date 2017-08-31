using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.AspNetCore.FluentRouter.Invoker.MSInternal
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

    public struct CoercedAwaitableInfo
    {
        public AwaitableInfo AwaitableInfo { get; }
        public Expression CoercerExpression { get; }
        public Type CoercerResultType { get; }
        public bool RequiresCoercion => CoercerExpression != null;

        public CoercedAwaitableInfo(AwaitableInfo awaitableInfo)
        {
            AwaitableInfo = awaitableInfo;
            CoercerExpression = null;
            CoercerResultType = null;
        }

        public CoercedAwaitableInfo(Expression coercerExpression, Type coercerResultType, AwaitableInfo coercedAwaitableInfo)
        {
            CoercerExpression = coercerExpression;
            CoercerResultType = coercerResultType;
            AwaitableInfo = coercedAwaitableInfo;
        }

        public static bool TryGetAwaitableInfo(Type type, out CoercedAwaitableInfo info)
        {
            if (AwaitableInfo.IsTypeAwaitable(type, out AwaitableInfo directlyAwaitableInfo))
            {
                info = new CoercedAwaitableInfo(directlyAwaitableInfo);
                return true;
            }

            // It's not directly awaitable, but maybe we can coerce it.
            // Currently we support coercing FSharpAsync<T>.
            if (ObjectMethodExecutorFSharpSupport.TryBuildCoercerFromFSharpAsyncToAwaitable(type,
                out var coercerExpression,
                out var coercerResultType))
            {
                if (AwaitableInfo.IsTypeAwaitable(coercerResultType, out AwaitableInfo coercedAwaitableInfo))
                {
                    info = new CoercedAwaitableInfo(coercerExpression, coercerResultType, coercedAwaitableInfo);
                    return true;
                }
            }

            info = default(CoercedAwaitableInfo);
            return false;
        }
    }

    public struct AwaitableInfo
    {
        public Type AwaiterType { get; }
        public PropertyInfo AwaiterIsCompletedProperty { get; }
        public MethodInfo AwaiterGetResultMethod { get; }
        public MethodInfo AwaiterOnCompletedMethod { get; }
        public MethodInfo AwaiterUnsafeOnCompletedMethod { get; }
        public Type ResultType { get; }
        public MethodInfo GetAwaiterMethod { get; }

        public AwaitableInfo(
            Type awaiterType,
            PropertyInfo awaiterIsCompletedProperty,
            MethodInfo awaiterGetResultMethod,
            MethodInfo awaiterOnCompletedMethod,
            MethodInfo awaiterUnsafeOnCompletedMethod,
            Type resultType,
            MethodInfo getAwaiterMethod)
        {
            AwaiterType = awaiterType;
            AwaiterIsCompletedProperty = awaiterIsCompletedProperty;
            AwaiterGetResultMethod = awaiterGetResultMethod;
            AwaiterOnCompletedMethod = awaiterOnCompletedMethod;
            AwaiterUnsafeOnCompletedMethod = awaiterUnsafeOnCompletedMethod;
            ResultType = resultType;
            GetAwaiterMethod = getAwaiterMethod;
        }

        public static bool IsTypeAwaitable(Type type, out AwaitableInfo awaitableInfo)
        {
            // Based on Roslyn code: http://source.roslyn.io/#Microsoft.CodeAnalysis.Workspaces/Shared/Extensions/ISymbolExtensions.cs,db4d48ba694b9347

            // Awaitable must have method matching "object GetAwaiter()"
            MethodInfo getAwaiterMethod = type.GetRuntimeMethods().FirstOrDefault(m =>
                m.Name.Equals("GetAwaiter", StringComparison.OrdinalIgnoreCase)
                && m.GetParameters().Length == 0
                && m.ReturnType != null);
            if (getAwaiterMethod == null)
            {
                awaitableInfo = default(AwaitableInfo);
                return false;
            }

            Type awaiterType = getAwaiterMethod.ReturnType;

            // Awaiter must have property matching "bool IsCompleted { get; }"
            PropertyInfo isCompletedProperty = awaiterType.GetRuntimeProperties().FirstOrDefault(p =>
                p.Name.Equals("IsCompleted", StringComparison.OrdinalIgnoreCase)
                && p.PropertyType == typeof(bool)
                && p.GetMethod != null);
            if (isCompletedProperty == null)
            {
                awaitableInfo = default(AwaitableInfo);
                return false;
            }

            // Awaiter must implement INotifyCompletion
            Type[] awaiterInterfaces = awaiterType.GetInterfaces();
            bool implementsINotifyCompletion = awaiterInterfaces.Any(t => t == typeof(INotifyCompletion));
            if (!implementsINotifyCompletion)
            {
                awaitableInfo = default(AwaitableInfo);
                return false;
            }

            // INotifyCompletion supplies a method matching "void OnCompleted(Action action)"
            InterfaceMapping iNotifyCompletionMap = awaiterType
                .GetTypeInfo()
                .GetRuntimeInterfaceMap(typeof(INotifyCompletion));
            MethodInfo onCompletedMethod = iNotifyCompletionMap.InterfaceMethods.Single(m =>
                m.Name.Equals("OnCompleted", StringComparison.OrdinalIgnoreCase)
                && m.ReturnType == typeof(void)
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(Action));

            // Awaiter optionally implements ICriticalNotifyCompletion
            bool implementsICriticalNotifyCompletion = awaiterInterfaces.Any(t => t == typeof(ICriticalNotifyCompletion));
            MethodInfo unsafeOnCompletedMethod;
            if (implementsICriticalNotifyCompletion)
            {
                // ICriticalNotifyCompletion supplies a method matching "void UnsafeOnCompleted(Action action)"
                InterfaceMapping iCriticalNotifyCompletionMap = awaiterType
                    .GetTypeInfo()
                    .GetRuntimeInterfaceMap(typeof(ICriticalNotifyCompletion));
                unsafeOnCompletedMethod = iCriticalNotifyCompletionMap.InterfaceMethods.Single(m =>
                    m.Name.Equals("UnsafeOnCompleted", StringComparison.OrdinalIgnoreCase)
                    && m.ReturnType == typeof(void)
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(Action));
            }
            else
            {
                unsafeOnCompletedMethod = null;
            }

            // Awaiter must have method matching "void GetResult" or "T GetResult()"
            MethodInfo getResultMethod = awaiterType.GetRuntimeMethods().FirstOrDefault(m =>
                m.Name.Equals("GetResult")
                && m.GetParameters().Length == 0);
            if (getResultMethod == null)
            {
                awaitableInfo = default(AwaitableInfo);
                return false;
            }

            awaitableInfo = new AwaitableInfo(
                awaiterType,
                isCompletedProperty,
                getResultMethod,
                onCompletedMethod,
                unsafeOnCompletedMethod,
                getResultMethod.ReturnType,
                getAwaiterMethod);
            return true;
        }
    }

    /// <summary>
    /// Helper for detecting whether a given type is FSharpAsync`1, and if so, supplying
    /// an <see cref="Expression"/> for mapping instances of that type to a C# awaitable.
    /// </summary>
    /// <remarks>
    /// The main design goal here is to avoid taking a compile-time dependency on
    /// FSharp.Core.dll, because non-F# applications wouldn't use it. So all the references
    /// to FSharp types have to be constructed dynamically at runtime.
    /// </remarks>
    internal static class ObjectMethodExecutorFSharpSupport
    {
        private static object _fsharpValuesCacheLock = new object();
        private static Assembly _fsharpCoreAssembly;
        private static MethodInfo _fsharpAsyncStartAsTaskGenericMethod;
        private static PropertyInfo _fsharpOptionOfTaskCreationOptionsNoneProperty;
        private static PropertyInfo _fsharpOptionOfCancellationTokenNoneProperty;

        public static bool TryBuildCoercerFromFSharpAsyncToAwaitable(
            Type possibleFSharpAsyncType,
            out Expression coerceToAwaitableExpression,
            out Type awaitableType)
        {
            var methodReturnGenericType = possibleFSharpAsyncType.IsGenericType
                ? possibleFSharpAsyncType.GetGenericTypeDefinition()
                : null;

            if (!IsFSharpAsyncOpenGenericType(methodReturnGenericType))
            {
                coerceToAwaitableExpression = null;
                awaitableType = null;
                return false;
            }

            var awaiterResultType = possibleFSharpAsyncType.GetGenericArguments().Single();
            awaitableType = typeof(Task<>).MakeGenericType(awaiterResultType);

            // coerceToAwaitableExpression = (object fsharpAsync) =>
            // {
            //     return (object)FSharpAsync.StartAsTask<TResult>(
            //         (Microsoft.FSharp.Control.FSharpAsync<TResult>)fsharpAsync,
            //         FSharpOption<TaskCreationOptions>.None,
            //         FSharpOption<CancellationToken>.None);
            // };
            var startAsTaskClosedMethod = _fsharpAsyncStartAsTaskGenericMethod
                .MakeGenericMethod(awaiterResultType);
            var coerceToAwaitableParam = Expression.Parameter(typeof(object));
            coerceToAwaitableExpression = Expression.Lambda(
                Expression.Convert(
                    Expression.Call(
                        startAsTaskClosedMethod,
                        Expression.Convert(coerceToAwaitableParam, possibleFSharpAsyncType),
                        Expression.MakeMemberAccess(null, _fsharpOptionOfTaskCreationOptionsNoneProperty),
                        Expression.MakeMemberAccess(null, _fsharpOptionOfCancellationTokenNoneProperty)),
                    typeof(object)),
                coerceToAwaitableParam);

            return true;
        }

        private static bool IsFSharpAsyncOpenGenericType(Type possibleFSharpAsyncGenericType)
        {
            var typeFullName = possibleFSharpAsyncGenericType?.FullName;
            if (!string.Equals(typeFullName, "Microsoft.FSharp.Control.FSharpAsync`1", StringComparison.Ordinal))
            {
                return false;
            }

            lock (_fsharpValuesCacheLock)
            {
                if (_fsharpCoreAssembly != null)
                {
                    // Since we've already found the real FSharpAsync.Core assembly, we just have
                    // to check that the supplied FSharpAsync`1 type is the one from that assembly.
                    return possibleFSharpAsyncGenericType.Assembly == _fsharpCoreAssembly;
                }
                else
                {
                    // We'll keep trying to find the FSharp types/values each time any type called
                    // FSharpAsync`1 is supplied.
                    return TryPopulateFSharpValueCaches(possibleFSharpAsyncGenericType);
                }
            }
        }

        private static bool TryPopulateFSharpValueCaches(Type possibleFSharpAsyncGenericType)
        {
            var assembly = possibleFSharpAsyncGenericType.Assembly;
            var fsharpOptionType = assembly.GetType("Microsoft.FSharp.Core.FSharpOption`1");
            var fsharpAsyncType = assembly.GetType("Microsoft.FSharp.Control.FSharpAsync");

            if (fsharpOptionType == null || fsharpAsyncType == null)
            {
                return false;
            }

            // Get a reference to FSharpOption<TaskCreationOptions>.None
            var fsharpOptionOfTaskCreationOptionsType = fsharpOptionType
                .MakeGenericType(typeof(TaskCreationOptions));
            _fsharpOptionOfTaskCreationOptionsNoneProperty = fsharpOptionOfTaskCreationOptionsType
                .GetTypeInfo()
                .GetRuntimeProperty("None");

            // Get a reference to FSharpOption<CancellationToken>.None
            var fsharpOptionOfCancellationTokenType = fsharpOptionType
                .MakeGenericType(typeof(CancellationToken));
            _fsharpOptionOfCancellationTokenNoneProperty = fsharpOptionOfCancellationTokenType
                .GetTypeInfo()
                .GetRuntimeProperty("None");

            // Get a reference to FSharpAsync.StartAsTask<>
            var fsharpAsyncMethods = fsharpAsyncType
                .GetRuntimeMethods()
                .Where(m => m.Name.Equals("StartAsTask", StringComparison.Ordinal));
            foreach (var candidateMethodInfo in fsharpAsyncMethods)
            {
                var parameters = candidateMethodInfo.GetParameters();
                if (parameters.Length == 3
                    && TypesHaveSameIdentity(parameters[0].ParameterType, possibleFSharpAsyncGenericType)
                    && parameters[1].ParameterType == fsharpOptionOfTaskCreationOptionsType
                    && parameters[2].ParameterType == fsharpOptionOfCancellationTokenType)
                {
                    // This really does look like the correct method (and hence assembly).
                    _fsharpAsyncStartAsTaskGenericMethod = candidateMethodInfo;
                    _fsharpCoreAssembly = assembly;
                    break;
                }
            }

            return _fsharpCoreAssembly != null;
        }

        private static bool TypesHaveSameIdentity(Type type1, Type type2)
        {
            return type1.Assembly == type2.Assembly
                && string.Equals(type1.Namespace, type2.Namespace, StringComparison.Ordinal)
                && string.Equals(type1.Name, type2.Name, StringComparison.Ordinal);
        }
    }
}