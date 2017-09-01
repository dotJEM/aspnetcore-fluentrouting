using System.Collections.Generic;
using DotJEM.AspNetCore.FluentRouting.Builders;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace DotJEM.AspNetCore.FluentRouting.Routing
{
    public interface IFluentActionDescriptorCache
    {
        IEnumerable<ActionDescriptor> Lookup(ControllerRoute route);
        IEnumerable<ActionDescriptor> Lookup(LambdaRoute route);
        
    }
}