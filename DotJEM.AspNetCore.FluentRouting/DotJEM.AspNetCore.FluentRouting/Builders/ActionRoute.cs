using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;

namespace DotJEM.AspNetCore.FluentRouter
{
    public class ActionRoute : Route
    {
        public Delegate Delegate { get; }

        public ActionRoute(Delegate @delegate, IRouter target, string routeName, string routeTemplate, RouteValueDictionary defaults, IDictionary<string, object> constraints, RouteValueDictionary dataTokens, IInlineConstraintResolver inlineConstraintResolver)
            : base(target, routeName, routeTemplate, defaults, constraints, dataTokens, inlineConstraintResolver)
        {
            this.Delegate = @delegate;
        }
    }
}