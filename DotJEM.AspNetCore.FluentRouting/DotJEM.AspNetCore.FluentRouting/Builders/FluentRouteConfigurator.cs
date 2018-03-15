using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace DotJEM.AspNetCore.FluentRouting.Builders
{
    public interface INamedFluentRouteConfigurator
    {
        // Note: Controller Routes:
        IFluentRouteBuilder To<TController>();

        // Note: Lanbda Routes:
        IFluentRouteBuilder To<TResult>(Func<HttpContext, TResult> handler);
        IFluentRouteBuilder To<TResult>(Func<TResult> handler);
        IFluentRouteBuilder To<T1, TResult>(Func<T1, TResult> handler);
        IFluentRouteBuilder To<T1, T2, TResult>(Func<T1, T2, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> handler);
        IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> handler);

        IFluentRouteBuilder ToDelegate(Delegate handler);

        IFluentRouteBuilder Through();
    }

    public interface IFluentRouteConfigurator : INamedFluentRouteConfigurator
    {
        INamedFluentRouteConfigurator Named(string name);
    }

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

        public IFluentRouteBuilder To<TResult>(Func<HttpContext, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<TResult>(Func<TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, TResult>(Func<T1, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, TResult>(Func<T1, T2, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> handler) => ToDelegate(handler);
        public IFluentRouteBuilder To<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> handler) => ToDelegate(handler);

        public IFluentRouteBuilder ToDelegate(Delegate handler)
        {
            return builder.AddDelegateRoute(
                handler,
                Name,
                Template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens));
        }

        public IFluentRouteBuilder Through()
        {
            return builder.AddIgnoreRoute(
                Name,
                Template);
        }

        public INamedFluentRouteConfigurator Named(string name)
        {
            Name = name;
            return this;
        }
    }

    public static class FluentRouteConfiguratorExtensions
    {

        //public IFluentRouteBuilder To<T1, TResult>(Func<T1, TResult> handler) => To(handler);
        //public IFluentRouteBuilder To<T1, T2, TResult>(Func<T1, T2, TResult> handler) => To(handler);
        //public IFluentRouteBuilder To<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> handler) => To(handler);
        //public IFluentRouteBuilder To<TResult>(Func<TResult> handler) => To(handler);
        //public IFluentRouteBuilder To<TResult>(Func<HttpContext, TResult> handler) => To(handler);
    }
}