using System;

namespace DotJEM.AspNetCore.FluentRouting.Builders.RouteObjects
{
    /// <summary>
    /// Constitudes a route that provides a globally unique identifier.
    /// </summary>
    public interface IIdentifiableRoute
    {
        Guid Id { get; }
    }
}