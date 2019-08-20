using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotJEM.AspNetCore.FluentRouting.Builders;
using DotJEM.AspNetCore.FluentRouting.Builders.RouteObjects;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace DotJEM.AspNetCore.FluentRouting.Routing
{
    public interface IFluentActionDescriptorCache
    {
        IEnumerable<ActionDescriptor> Lookup(ControllerRoute route);
        IEnumerable<ActionDescriptor> Lookup(LambdaRoute route);
    }

    public class FluentActionDescriptorCache : IFluentActionDescriptorCache
    {
        private static readonly ActionDescriptor[] EMPTY_RESULT = new ActionDescriptor[0];

        private readonly IFluentActionDescriptorFactory factory;
        private readonly ConcurrentDictionary<Guid, IList<ActionDescriptor>> cache = new ConcurrentDictionary<Guid, IList<ActionDescriptor>>();
        
        public FluentActionDescriptorCache(IFluentActionDescriptorFactory factory)
        {
            this.factory = factory;
        }

        public IEnumerable<ActionDescriptor> Lookup(ControllerRoute route) => GetOrAdd(route, () => factory.CreateDescriptors(route));
        public IEnumerable<ActionDescriptor> Lookup(LambdaRoute route) => GetOrAdd(route, () => factory.CreateDescriptors(route));
        private IEnumerable<ActionDescriptor> GetOrAdd(IIdentifiableRoute route, Func<IEnumerable<ActionDescriptor>> factory)
        {
            return route == null ? EMPTY_RESULT : cache.GetOrAdd(route.Id, s => factory().ToList());
        }
    }
}