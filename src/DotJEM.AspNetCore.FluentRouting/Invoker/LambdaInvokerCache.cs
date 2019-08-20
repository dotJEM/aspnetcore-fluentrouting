using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotJEM.AspNetCore.FluentRouting.Invoker.Execution;
using DotJEM.AspNetCore.FluentRouting.Invoker.MSInternal;
using DotJEM.AspNetCore.FluentRouting.Routing.Lambdas;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DotJEM.AspNetCore.FluentRouting.Invoker
{
    public class LambdaInvokerCache
    {
        private readonly ILambdaBinderDelegateFactory lambdaBinderDelegateFactory;
        private readonly IFilterProvider[] filterProviders;
        private readonly ILambdaExecutorDelegateFactory delegateFactory = new LambdaExecutorDelegateFactory();

        private readonly ConcurrentDictionary<ActionDescriptor, LambdaInvokerCacheEntry> cache =
            new ConcurrentDictionary<ActionDescriptor, LambdaInvokerCacheEntry>();

        public LambdaInvokerCache(
            ILambdaBinderDelegateFactory lambdaBinderDelegateFactory,
            IEnumerable<IFilterProvider> filterProviders)
        {
            this.lambdaBinderDelegateFactory = lambdaBinderDelegateFactory;
            this.filterProviders = filterProviders.OrderBy(item => item.Order).ToArray();
        }

        public (LambdaInvokerCacheEntry, IFilterMetadata[]) Lookup(LambdaActionContext context)
        {
            LambdaDescriptor descriptor = context.ActionDescriptor as LambdaDescriptor;
            if (descriptor == null)
                return (null, null);

            if (!cache.TryGetValue(descriptor, out LambdaInvokerCacheEntry entry))
            {
                FilterFactoryResult filterFactoryResult = FilterFactory.GetAllFilters(filterProviders, context);
                IFilterMetadata[] filterMetadatas = filterFactoryResult.Filters;

                //NOTE: How do we provide Default parameters for Func parameters?...
                //object[] parameterDefaultValues = ParameterDefaultValues.GetParameterDefaultValues(descriptor.Delegate.Method);

                LambdaExecutor executor;
                if (CoercedAwaitableInfo.TryGetAwaitableInfo(descriptor.Delegate.Method.ReturnType,
                    out CoercedAwaitableInfo awaitableInfo))
                {
                    executor = new AsyncLambdaExecutor(
                        descriptor.Delegate,
                        delegateFactory.Create(descriptor.Delegate),
                        delegateFactory.CreateAsync(descriptor.Delegate, awaitableInfo),
                        awaitableInfo.AwaitableInfo.ResultType);
                }
                else
                {
                    executor = new SyncLambdaExecutor(
                        descriptor.Delegate,
                        delegateFactory.Create(descriptor.Delegate));
                }

                //TODO: Inject service instead.
                LambdaBinderDelegate lambdaBinder = lambdaBinderDelegateFactory.CreateBinderDelegate(descriptor);

                return (cache.GetOrAdd(descriptor, new LambdaInvokerCacheEntry(lambdaBinder, executor, filterFactoryResult.CacheableFilters)), filterMetadatas);
            }
            return (entry, FilterFactory.CreateUncachedFilters(filterProviders, context, entry.CacheableFilters));
        }
    }
}