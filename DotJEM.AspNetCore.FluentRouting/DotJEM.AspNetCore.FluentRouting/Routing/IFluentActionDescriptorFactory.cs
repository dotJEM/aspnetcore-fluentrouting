using System.Collections.Generic;
using DotJEM.AspNetCore.FluentRouting.Builders;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace DotJEM.AspNetCore.FluentRouting.Routing
{
    public interface IFluentActionDescriptorFactory
    {
        IEnumerable<ActionDescriptor> CreateDescriptors(ControllerRoute route);
        IEnumerable<ActionDescriptor> CreateDescriptors(LambdaRoute route);
    }
}