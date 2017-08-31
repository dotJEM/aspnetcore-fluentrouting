namespace DotJEM.AspNetCore.FluentRouter
{
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
}