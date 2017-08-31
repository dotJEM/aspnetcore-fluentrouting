using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace DotJEM.AspNetCore.FluentRouter.Routing
{
    public interface IFluentActionDescriptorCache
    {
        IEnumerable<ActionDescriptor> Lookup(ControllerRoute route);
        IEnumerable<ActionDescriptor> Lookup(ActionRoute route);
        
    }
}