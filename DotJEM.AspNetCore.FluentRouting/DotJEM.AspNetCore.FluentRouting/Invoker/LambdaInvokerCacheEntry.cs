using DotJEM.AspNetCore.FluentRouting.Invoker.Execution;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DotJEM.AspNetCore.FluentRouting.Invoker
{
    public class LambdaInvokerCacheEntry
    {
        public LambdaBinderDelegate LambdaBinderDelegate { get; }
        public FilterItem[] CacheableFilters { get; }
        public LambdaExecutor ActionExecutor { get; }

        public LambdaInvokerCacheEntry(LambdaBinderDelegate lambdaBinder, LambdaExecutor executor, FilterItem[] cacheableFilters)
        {
            LambdaBinderDelegate = lambdaBinder;
            CacheableFilters = cacheableFilters;
            ActionExecutor = executor;
        }
    }
}