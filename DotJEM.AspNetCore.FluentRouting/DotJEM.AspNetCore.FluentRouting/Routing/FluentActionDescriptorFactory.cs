using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotJEM.AspNetCore.FluentRouting.Builders;
using DotJEM.AspNetCore.FluentRouting.Builders.RouteObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Options;

namespace DotJEM.AspNetCore.FluentRouting.Routing
{
    public class FluentActionDescriptorFactory : IFluentActionDescriptorFactory
    {
        private readonly IApplicationModelProvider[] providers;
        private readonly IEnumerable<IApplicationModelConvention> conventions;

        public FluentActionDescriptorFactory(IEnumerable<IApplicationModelProvider> applicationModelProviders, IOptions<MvcOptions> optionsAccessor)
        {
            this.providers = applicationModelProviders?.OrderBy(p => p.Order).ToArray() ?? throw new ArgumentNullException(nameof(applicationModelProviders));
            this.conventions = optionsAccessor?.Value.Conventions ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }

        public IEnumerable<ActionDescriptor> CreateDescriptors(ControllerRoute route)
        {
            ApplicationModel applicationModel = BuildModel(route.BoundControllerType.GetTypeInfo());
            ApplicationModelConventions.ApplyConventions(applicationModel, conventions);
            return ControllerActionDescriptorBuilder.Build(applicationModel);
        }

        public IEnumerable<ActionDescriptor> CreateDescriptors(LambdaRoute route)
        {
            return new []{ new LambdaDescriptor(route.Delegate) };
        }

        protected internal ApplicationModel BuildModel(TypeInfo controllerType)
        {
            ApplicationModelProviderContext context = new ApplicationModelProviderContext(new[] { controllerType });
            foreach (IApplicationModelProvider provider in providers) provider.OnProvidersExecuting(context);
            foreach (IApplicationModelProvider provider in providers.Reverse()) provider.OnProvidersExecuted(context);
            return context.Result;
        }


    }
}