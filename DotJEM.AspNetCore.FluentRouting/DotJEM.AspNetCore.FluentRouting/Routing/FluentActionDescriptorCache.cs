using System.Collections.Generic;
using DotJEM.AspNetCore.FluentRouting.Builders;
using DotJEM.AspNetCore.FluentRouting.Builders.RouteObjects;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace DotJEM.AspNetCore.FluentRouting.Routing
{
    public class FluentActionDescriptorCache : IFluentActionDescriptorCache
    {
        private readonly IFluentActionDescriptorFactory factory;

        public FluentActionDescriptorCache(IFluentActionDescriptorFactory factory)
        {
            this.factory = factory;
        }

        public IEnumerable<ActionDescriptor> Lookup(ControllerRoute route)
        {
            if(route == null) return new ActionDescriptor[0];
            //TODO : CACHE
            return factory.CreateDescriptors(route);
        }

        public IEnumerable<ActionDescriptor> Lookup(LambdaRoute route)
        {
            if(route == null) return new ActionDescriptor[0];
            //TODO : CACHE
            return factory.CreateDescriptors(route);
        }
    }
}