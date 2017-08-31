using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotJEM.AspNetCore.FluentRouter.Builders
{
    // https://github.com/aspnet/Home
    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing
    // https://github.com/ivaylokenov/AspNet.Mvc.TypedRouting
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/routing

    //TODO: Perhaps switch to MvC and inherit the MvcHandler, or we need to add things deeper.


    public class FluentRouteBuilder : IFluentRouteBuilder
    {
        private readonly IApplicationBuilder app;
        private readonly IRouter handler;

        private readonly List<IRouter> routes = new List<IRouter>();

        public FluentRouteBuilder(IApplicationBuilder app, IRouter handler)
        {
            this.app = app ?? throw new ArgumentNullException(nameof(app));
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public IFluentRouteConfigurator Route(string template)
        {
            return new FluentRouteConfigurator(this, template);
        }

        public IFluentRouteBuilder AddControllerRoute<TController>(string name, string template, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens)
        {
            return AddControllerRoute(typeof(TController), name, template, defaults, constraints, dataTokens);
        }

        public IFluentRouteBuilder AddControllerRoute(Type controllerType, string name, string template, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens)
        {
            IInlineConstraintResolver resolver = app.ApplicationServices.GetRequiredService<IInlineConstraintResolver>();
            routes.Add(new ControllerRoute(controllerType, handler, name, template, defaults, constraints, dataTokens, resolver));
            return this;
        }
        public IFluentRouteBuilder AddDelegateRoute(Delegate handler, string name, string template, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens)
        {
            IInlineConstraintResolver resolver = app.ApplicationServices.GetRequiredService<IInlineConstraintResolver>();
            routes.Add(new ActionRoute(handler, this.handler, name, template, defaults, constraints, dataTokens, resolver));
            return this;
        }

        public IRouter Build()
        {
            RouteCollection routeCollection = new RouteCollection();
            routes.ForEach(routeCollection.Add);
            return routeCollection;
        }

        //public RouteBuilder(IApplicationBuilder applicationBuilder, IRouter defaultHandler)
        //{
        //    if (applicationBuilder == null)
        //        throw new ArgumentNullException("applicationBuilder");
        //    if (applicationBuilder.ApplicationServices.GetService(typeof(RoutingMarkerService)) == null)
        //        throw new InvalidOperationException(Resources.FormatUnableToFindServices((object)"IServiceCollection", (object)"AddRouting", (object)"ConfigureServices(...)"));
        //    this.ApplicationBuilder = applicationBuilder;
        //    this.DefaultHandler = defaultHandler;
        //    this.ServiceProvider = applicationBuilder.ApplicationServices;
        //    this.Routes = (IList<IRouter>)new List<IRouter>();
        //}

        //public IRouter Build()
        //{
        //    RouteCollection routeCollection = new RouteCollection();
        //    foreach (IRouter route in (IEnumerable<IRouter>)this.Routes)
        //        routeCollection.Add(route);
        //    return (IRouter)routeCollection;
        //}
    }

    //public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, object constraints, object dataTokens)
    //{
    //    if (routeBuilder.DefaultHandler == null)
    //    throw new RouteCreationException(Resources.FormatDefaultHandler_MustBeSet((object)"IRouteBuilder"));
    //    IInlineConstraintResolver requiredService = routeBuilder.ServiceProvider.GetRequiredService<IInlineConstraintResolver>();
    //    routeBuilder.Routes.Add((IRouter)new Route(routeBuilder.DefaultHandler, name, template, new RouteValueDictionary(defaults), (IDictionary<string, object>)new RouteValueDictionary(constraints), new RouteValueDictionary(dataTokens), requiredService));
    //    return routeBuilder;
    //}
}

