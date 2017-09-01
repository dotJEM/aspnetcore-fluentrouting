using System;
using Microsoft.AspNetCore.Http;

namespace DotJEM.AspNetCore.FluentRouter
{
    public interface INamedFluentRouteConfigurator
    {
        IFluentRouteBuilder To<TController>();
        IFluentRouteBuilder To<TResult>(Func<TResult> handler);
        IFluentRouteBuilder To<T1, TResult>(Func<T1,TResult> handler);
        IFluentRouteBuilder To<T1, T2, TResult>(Func<T1,T2,TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, TResult>(Func<T1,T2, T3, T4, TResult> handler);
        IFluentRouteBuilder To<TResult>(Func<HttpContext, TResult> handler);
        //IFluentRouteBuilder To<TContext, TDependency, TResult>(Func<TContext, TDependency, TResult> handler) where TContext: FluentContext;
        //IFluentRouteBuilder To<TContext>(Action<TContext> handler) where TContext : FluentContext;
        //IFluentRouteBuilder To<TContext, TDependency>(Action<TContext, TDependency> handler) where TContext : FluentContext;
    }
}