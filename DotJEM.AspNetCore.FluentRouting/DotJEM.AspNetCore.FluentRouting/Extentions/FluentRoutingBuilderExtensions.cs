using System;
using DotJEM.AspNetCore.FluentRouting.Builders;
using DotJEM.AspNetCore.FluentRouting.Routing;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    public static class FluentRoutingBuilderExtensions
    {
        public static IApplicationBuilder UseFluentRouter(this IApplicationBuilder app, Action<IFluentRouteBuilder> config)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            FluentRouteBuilder builder = new FluentRouteBuilder(app, app.ApplicationServices.GetRequiredService<FluentRouteHandler>());

            config(builder);

            return app.UseRouter(builder.Build());
        }



        //public static IApplicationBuilder UseRouter(this IApplicationBuilder builder, Action<IRouteBuilder> action)
        //{
        //    if (builder == null)
        //        throw new ArgumentNullException("builder");
        //    if (action == null)
        //        throw new ArgumentNullException("action");
        //    if (builder.ApplicationServices.GetService(typeof(RoutingMarkerService)) == null)
        //        throw new InvalidOperationException(Resources.FormatUnableToFindServices((object)"IServiceCollection", (object)"AddRouting", (object)"ConfigureServices(...)"));
        //    RouteBuilder routeBuilder = new RouteBuilder(builder);
        //    action((IRouteBuilder)routeBuilder);
        //    return builder.UseRouter(routeBuilder.Build());
        //}

        //public static IApplicationBuilder UseMvc(this IApplicationBuilder app, Action<IRouteBuilder> configureRoutes)
        //{
        //    if (app == null)
        //        throw new ArgumentNullException("app");
        //    if (configureRoutes == null)
        //        throw new ArgumentNullException("configureRoutes");
        //    if (app.ApplicationServices.GetService(typeof(MvcMarkerService)) == null)
        //        throw new InvalidOperationException(Microsoft.AspNetCore.Mvc.Core.Resources.FormatUnableToFindServices((object)"IServiceCollection", (object)"AddMvc", (object)"ConfigureServices(...)"));
        //    app.ApplicationServices.GetRequiredService<MiddlewareFilterBuilder>().ApplicationBuilder = app.New();
        //    RouteBuilder routeBuilder = new RouteBuilder(app)
        //    {
        //        DefaultHandler = (IRouter)app.ApplicationServices.GetRequiredService<MvcRouteHandler>()
        //    };
        //    configureRoutes((IRouteBuilder)routeBuilder);
        //    routeBuilder.Routes.Insert(0, AttributeRouting.CreateAttributeMegaRoute(app.ApplicationServices));
        //    return app.UseRouter(routeBuilder.Build());
        //}
    }
}