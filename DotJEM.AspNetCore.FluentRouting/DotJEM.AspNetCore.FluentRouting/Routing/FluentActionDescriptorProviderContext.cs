using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace DotJEM.AspNetCore.FluentRouter.Routing
{
    public class FluentActionDescriptorProviderContext : ActionDescriptorProviderContext
    {
        private readonly ControllerRoute[] routes;

        public IEnumerable<TypeInfo> GetControllerTypes()
        {
            //TODO: Cache
            return routes.Select(route => route.BoundControllerType.GetTypeInfo());
        }

        public FluentActionDescriptorProviderContext(ControllerRoute[] routes)
        {
            this.routes = routes;
        }
    }
}