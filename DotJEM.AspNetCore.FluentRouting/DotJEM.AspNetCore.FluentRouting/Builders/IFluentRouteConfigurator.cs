namespace DotJEM.AspNetCore.FluentRouter
{
    public interface IFluentRouteConfigurator : INamedFluentRouteConfigurator
    {
        INamedFluentRouteConfigurator Named(string name);
    }
}