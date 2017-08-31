using System;
using DotJEM.AspNetCore.FluentRouter.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DotJEM.AspNetCore.FluentRouter
{
    public class FluentRouteConfigurator : IFluentRouteConfigurator
    {
        private readonly FluentRouteBuilder builder;

        public string Name { get; private set; }
        public string Template { get; }

        private object defaults, constraints, dataTokens;

        public FluentRouteConfigurator(FluentRouteBuilder builder, string template)
        {
            Template = template;
            this.builder = builder;
        }

        public IFluentRouteBuilder To<TController>()
        {
            return builder.AddControllerRoute<TController>(
                Name,
                Template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens));

        }

        public IFluentRouteBuilder To<T1, TResult>(Func<T1, TResult> handler)
        {
            return builder.AddDelegateRoute(
                handler,
                Name,
                Template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens));
        }

        public IFluentRouteBuilder To<T1, T2, TResult>(Func<T1, T2, TResult> handler)
        {
            return builder.AddDelegateRoute(
                handler,
                Name,
                Template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens));
        }

        public IFluentRouteBuilder To<TResult>(Func<TResult> handler)
        {
            return builder.AddDelegateRoute(
                handler,
                Name,
                Template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens));
        }

        public IFluentRouteBuilder To<TResult>(Func<HttpContext, TResult> handler)
        {
            return builder.AddDelegateRoute(
                handler,
                Name,
                Template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens));
        }

        //public IFluentRouteBuilder To<TContext, TDependency, TResult>(Func<TContext, TDependency, TResult> handler) where TContext : FluentContext
        //{
        //    throw new NotImplementedException();
        //}

        //public IFluentRouteBuilder To<TContext>(Action<TContext> handler) where TContext : FluentContext
        //{
        //    throw new NotImplementedException();
        //}

        //public IFluentRouteBuilder To<TContext, TDependency>(Action<TContext, TDependency> handler) where TContext : FluentContext
        //{
        //    throw new NotImplementedException();
        //}


        public INamedFluentRouteConfigurator Named(string name)
        {
            Name = name;
            return this;
        }
    }
}