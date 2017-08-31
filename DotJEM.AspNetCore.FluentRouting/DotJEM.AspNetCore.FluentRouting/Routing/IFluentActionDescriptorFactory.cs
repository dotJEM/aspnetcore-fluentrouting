using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace DotJEM.AspNetCore.FluentRouter.Routing
{
    public interface IFluentActionDescriptorFactory
    {
        IEnumerable<ActionDescriptor> CreateDescriptors(ControllerRoute route);
        IEnumerable<ActionDescriptor> CreateDescriptors(ActionRoute route);
    }
}