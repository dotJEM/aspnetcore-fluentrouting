using System;
using System.Threading.Tasks;
using DotJEM.AspNetCore.FluentRouting.Invoker;
using DotJEM.AspNetCore.FluentRouting.Invoker.Execution;
using DotJEM.AspNetCore.FluentRouting.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotJEM.AspNetCore.FluentRouting.Extentions
{
    /// <summary>
    /// Contains extension methods to <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    public static class FluentRoutingServiceCollectionExtensions
    {
        /// <summary>Adds services required for routing requests.</summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddFluentRouting(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IActionSelector, FluentActionSelector>();

            services.AddMvcCore();

            services.TryAddSingleton<LambdaInvokerCache>();
            services.TryAddSingleton<FluentRouteHandler>();

            services.TryAddSingleton<IFluentActionDescriptorFactory, FluentActionDescriptorFactory>();
            services.TryAddSingleton<IFluentActionDescriptorCache, FluentActionDescriptorCache>();
            services.TryAddSingleton<ILambdaExecutorDelegateFactory, LambdaExecutorDelegateFactory>();

            services.TryAddEnumerable(ServiceDescriptor.Transient<IActionInvokerProvider, LambdaInvokerProvider>());
            //LambdaInvokerProvider: IActionInvokerProvider
            //services.TryAddTransient<IInlineConstraintResolver, DefaultInlineConstraintResolver>();
            //services.TryAddSingleton<UrlEncoder>(UrlEncoder.Default);
            //services.TryAddSingleton<Microsoft.Extensions.ObjectPool.ObjectPool<UriBuildingContext>>((Func<IServiceProvider, Microsoft.Extensions.ObjectPool.ObjectPool<UriBuildingContext>>)(s => s.GetRequiredService<ObjectPoolProvider>().Create<UriBuildingContext>((IPooledObjectPolicy<UriBuildingContext>)new UriBuilderContextPooledObjectPolicy(s.GetRequiredService<UrlEncoder>()))));
            //services.TryAddTransient<TreeRouteBuilder>();
            //services.TryAddSingleton(typeof(RoutingMarkerService));
            return services;
        }

        /// <summary>Adds services required for routing requests.</summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
        /// <param name="configureOptions">The routing options to configure the middleware with.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddFluentRouting(this IServiceCollection services, Action<RouteOptions> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.AddFluentRouting();
            return services;
        }
    }

}