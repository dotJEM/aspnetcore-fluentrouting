using System;
using System.Collections.Generic;
using DotJEM.AspNetCore.FluentRouting.Builders.RouteObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotJEM.AspNetCore.FluentRouting.Builders
{
    // https://github.com/aspnet/Home
    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing
    // https://github.com/ivaylokenov/AspNet.Mvc.TypedRouting
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/routing

    public interface IFluentRouteBuilder
    {
        IFluentRouteConfigurator Route(string template);

        //IFluentRouteConfigurator Route(string verb, string template);
        //IFluentRouteConfigurator RouteConnect(string template);
        //IFluentRouteConfigurator RouteDelete(string template);
        //IFluentRouteConfigurator RouteGet(string template);
        //IFluentRouteConfigurator RouteHead(string template);
        //IFluentRouteConfigurator RouteOptions(string template);
        //IFluentRouteConfigurator RoutePatch(string template);
        //IFluentRouteConfigurator RoutePut(string template);
        //IFluentRouteConfigurator RouteTrace(string template);
    }

    public static class FluentRouteBuilderExt
    {
        public static IFluentRouteConfigurator Default(this IFluentRouteBuilder self) => self.Route("{*url}");
    }

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
        public IFluentRouteBuilder AddDelegateRoute(Delegate lambda, string name, string template, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens)
        {
            IInlineConstraintResolver resolver = app.ApplicationServices.GetRequiredService<IInlineConstraintResolver>();
            routes.Add(new LambdaRoute(lambda, handler, name, template, defaults, constraints, dataTokens, resolver));
            return this;
        }

        public IRouter Build()
        {
            RouteCollection routeCollection = new RouteCollection();
            routes.ForEach(routeCollection.Add);
            return routeCollection;
        }
    }

}

