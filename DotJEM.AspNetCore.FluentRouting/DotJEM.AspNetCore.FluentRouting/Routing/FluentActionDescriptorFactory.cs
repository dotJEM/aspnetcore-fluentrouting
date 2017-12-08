using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotJEM.AspNetCore.FluentRouting.Builders;
using DotJEM.AspNetCore.FluentRouting.Builders.RouteObjects;
using DotJEM.AspNetCore.FluentRouting.Routing.Lambdas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;

namespace DotJEM.AspNetCore.FluentRouting.Routing
{
    public interface IFluentActionDescriptorFactory
    {
        IEnumerable<ActionDescriptor> CreateDescriptors(ControllerRoute route);
        IEnumerable<ActionDescriptor> CreateDescriptors(LambdaRoute route);
    }
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
            ActionModel model = FixFaxModelConventions.CreateFluentActionModel(route.Delegate.Method);
            

            FixFaxModelConventions.ApplyConventions(model);

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

    public static class FixFaxModelConventions
    {
        public static void ApplyConventions(ApplicationModel applicationModel, IEnumerable<IApplicationModelConvention> conventions)
        {
            if (applicationModel == null) throw new ArgumentNullException(nameof(applicationModel));
            if (conventions == null) throw new ArgumentNullException(nameof(conventions));


            foreach (IApplicationModelConvention convention in conventions)
                convention.Apply(applicationModel);


            foreach (ControllerModel controller in applicationModel.Controllers)
            {
                foreach (IControllerModelConvention controllerModelConvention in controller.Attributes.OfType<IControllerModelConvention>())
                    controllerModelConvention.Apply(controller);
                foreach (ActionModel action in controller.Actions)
                {
                    ApplyConventions(action);
                }
            }
        }

        public static void ApplyConventions(ActionModel action)
        {
            foreach (IActionModelConvention actionModelConvention in action.Attributes.OfType<IActionModelConvention>())
                actionModelConvention.Apply(action);

            foreach (ParameterModel parameter in action.Parameters)
            {
                foreach (IParameterModelConvention parameterModelConvention in parameter.Attributes.OfType<IParameterModelConvention>())
                    parameterModelConvention.Apply(parameter);
            }
        }

        public static ActionModel CreateFluentActionModel(MethodInfo methodInfo)
        {
            //Note: Always empty?
            //object[] customAttributes = methodInfo.GetCustomAttributes(true);

            ActionModel actionModel = new ActionModel(methodInfo, new List<object>());

            //NOTE: All this seems irellevant for Lambda routes as they can't have custom attributes?
            //foreach (IFilterMetadata metadata in customAttributes.OfType<IFilterMetadata>())
            //    actionModel.Filters.Add(metadata);

            //ActionNameAttribute actionNameAttribute = customAttributes
            //    .OfType<ActionNameAttribute>()
            //    .FirstOrDefault();

            //actionModel.ActionName = actionNameAttribute?.Name ?? methodInfo.Name;
            //IApiDescriptionVisibilityProvider visibilityProvider = customAttributes
            //    .OfType<IApiDescriptionVisibilityProvider>()
            //    .FirstOrDefault();

            //if (visibilityProvider != null)
            //    actionModel.ApiExplorer.IsVisible = !visibilityProvider.IgnoreApi;

            //IApiDescriptionGroupNameProvider groupNameProvider = customAttributes
            //    .OfType<IApiDescriptionGroupNameProvider>()
            //    .FirstOrDefault();

            //if (groupNameProvider != null)
            //    actionModel.ApiExplorer.GroupName = groupNameProvider.GroupName;

            //foreach (IRouteValueProvider routeValueProvider in customAttributes.OfType<IRouteValueProvider>())
            //    actionModel.RouteValues.Add(routeValueProvider.RouteKey, routeValueProvider.RouteValue);

            //IRouteTemplateProvider[] array = methodInfo
            //    .GetCustomAttributes(false)
            //    .OfType<IRouteTemplateProvider>()
            //    .ToArray();

            //List<object> objectList = new List<object>();
            //foreach (object obj in customAttributes)
            //{
            //    if (!(obj is IRouteTemplateProvider))
            //        objectList.Add(obj);
            //}
            //objectList.AddRange(array);
            foreach (SelectorModel model in CreateSelectors(new List<object>()))
                actionModel.Selectors.Add(model);

            return actionModel;
        }

        private static IList<SelectorModel> CreateSelectors(IList<object> attributes)
        {
            // Route attributes create multiple selector models, we want to split the set of
            // attributes based on these so each selector only has the attributes that affect it.
            //
            // The set of route attributes are split into those that 'define' a route versus those that are
            // 'silent'.
            //
            // We need to define a selector for each attribute that 'defines' a route, and a single selector
            // for all of the ones that don't (if any exist).
            //
            // If the attribute that 'defines' a route is NOT an IActionHttpMethodProvider, then we'll include with
            // it, any IActionHttpMethodProvider that are 'silent' IRouteTemplateProviders. In this case the 'extra'
            // action for silent route providers isn't needed.
            //
            // Ex:
            // [HttpGet]
            // [AcceptVerbs("POST", "PUT")]
            // [HttpPost("Api/Things")]
            // public void DoThing()
            //
            // This will generate 2 selectors:
            // 1. [HttpPost("Api/Things")]
            // 2. [HttpGet], [AcceptVerbs("POST", "PUT")]
            //
            // Another example of this situation is:
            //
            // [Route("api/Products")]
            // [AcceptVerbs("GET", "HEAD")]
            // [HttpPost("api/Products/new")]
            //
            // This will generate 2 selectors:
            // 1. [AcceptVerbs("GET", "HEAD")]
            // 2. [HttpPost]
            //
            // Note that having a route attribute that doesn't define a route template _might_ be an error. We
            // don't have enough context to really know at this point so we just pass it on.
            List<IRouteTemplateProvider> routeProviders = new List<IRouteTemplateProvider>();

            bool createSelectorForSilentRouteProviders = false;
            foreach (object attribute in attributes)
            {
                if (!(attribute is IRouteTemplateProvider routeTemplateProvider))
                    continue;

                if (IsSilentRouteAttribute(routeTemplateProvider))
                    createSelectorForSilentRouteProviders = true;
                else
                    routeProviders.Add(routeTemplateProvider);
            }

            foreach (IRouteTemplateProvider routeProvider in routeProviders)
            {
                // If we see an attribute like
                // [Route(...)]
                //
                // Then we want to group any attributes like [HttpGet] with it.
                //
                // Basically...
                //
                // [HttpGet]
                // [HttpPost("Products")]
                // public void Foo() { }
                //
                // Is two selectors. And...
                //
                // [HttpGet]
                // [Route("Products")]
                // public void Foo() { }
                //
                // Is one selector.
                if (!(routeProvider is IActionHttpMethodProvider))
                    createSelectorForSilentRouteProviders = false;
            }

            List<SelectorModel> selectorModels = new List<SelectorModel>();
            if (routeProviders.Count == 0 && !createSelectorForSilentRouteProviders)
            {
                // Simple case, all attributes apply
                selectorModels.Add(CreateSelectorModel(route: null, attributes: attributes));
            }
            else
            {
                // Each of these routeProviders are the ones that actually have routing information on them
                // something like [HttpGet] won't show up here, but [HttpGet("Products")] will.
                foreach (IRouteTemplateProvider routeProvider in routeProviders)
                {
                    List<object> filteredAttributes = new List<object>();
                    foreach (object attribute in attributes)
                    {
                        if (ReferenceEquals(attribute, routeProvider))
                        {
                            filteredAttributes.Add(attribute);
                        }
                        else if (InRouteProviders(routeProviders, attribute))
                        {
                            // Exclude other route template providers
                            // Example:
                            // [HttpGet("template")]
                            // [Route("template/{id}")]
                        }
                        else if (routeProvider is IActionHttpMethodProvider && attribute is IActionHttpMethodProvider)
                        {
                            // Example:
                            // [HttpGet("template")]
                            // [AcceptVerbs("GET", "POST")]
                            //
                            // Exclude other http method providers if this route is an
                            // http method provider.
                        }
                        else
                        {
                            filteredAttributes.Add(attribute);
                        }
                    }

                    selectorModels.Add(CreateSelectorModel(routeProvider, filteredAttributes));
                }

                if (createSelectorForSilentRouteProviders)
                {
                    List<object> filteredAttributes = new List<object>();
                    foreach (object attribute in attributes)
                    {
                        if (!InRouteProviders(routeProviders, attribute))
                        {
                            filteredAttributes.Add(attribute);
                        }
                    }

                    selectorModels.Add(CreateSelectorModel(null, filteredAttributes));
                }
            }

            return selectorModels;
        }

        private static SelectorModel CreateSelectorModel(IRouteTemplateProvider route, IList<object> attributes)
        {
            SelectorModel selectorModel = new SelectorModel();
            if (route != null)
            {
                selectorModel.AttributeRouteModel = new AttributeRouteModel(route);
            }
            foreach (IActionConstraintMetadata metadata in attributes.OfType<IActionConstraintMetadata>())
                selectorModel.ActionConstraints.Add(metadata);

            // Simple case, all HTTP method attributes apply
            string[] httpMethods = attributes
                .OfType<IActionHttpMethodProvider>()
                .SelectMany(a => a.HttpMethods)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (httpMethods.Length > 0)
                selectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(httpMethods));

            return selectorModel;
        }
        private static bool InRouteProviders(List<IRouteTemplateProvider> routeProviders, object attribute)
        {
            return routeProviders
                .Any(rp => ReferenceEquals(rp, attribute));
        }

        private static bool IsSilentRouteAttribute(IRouteTemplateProvider routeTemplateProvider)
        {
            return
                routeTemplateProvider.Template == null &&
                routeTemplateProvider.Order == null &&
                routeTemplateProvider.Name == null;
        }
    }
}