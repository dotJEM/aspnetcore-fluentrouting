namespace DotJEM.AspNetCore.FluentRouting.Builders
{
    public interface IFluentRouteConfigurator : INamedFluentRouteConfigurator
    {
        INamedFluentRouteConfigurator Named(string name);
    }
}