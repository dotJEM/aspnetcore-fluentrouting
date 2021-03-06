using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;

namespace DotJEM.AspNetCore.FluentRouting.Builders.RouteObjects
{
    public class ControllerRoute : Route, IIdentifiableRoute
    {
        public Guid Id { get; } = Guid.NewGuid();

        public Type BoundControllerType { get; }
        public ControllerRoute(Type controllerType, IRouter target, string routeName, string routeTemplate, RouteValueDictionary defaults, IDictionary<string, object> constraints, RouteValueDictionary dataTokens, IInlineConstraintResolver inlineConstraintResolver)
            : base(target, routeName, routeTemplate, defaults, constraints, dataTokens, inlineConstraintResolver)
        {
            BoundControllerType = controllerType;
        }
    }
}