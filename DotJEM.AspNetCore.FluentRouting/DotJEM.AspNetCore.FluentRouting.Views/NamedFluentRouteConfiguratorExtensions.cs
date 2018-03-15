using System;
using DotJEM.AspNetCore.FluentRouting.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace DotJEM.AspNetCore.FluentRouting.Views
{
    public interface IViewDataProvider
    {
        ViewDataDictionary ViewData(string viewName, HttpContext context);
        ITempDataDictionary TempData(string viewName, HttpContext context);
    }

    public class NullViewDataProvider : IViewDataProvider
    {
        public ViewDataDictionary ViewData(string viewName, HttpContext context) 
            =>  new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = new { } };

        public ITempDataDictionary TempData(string viewName, HttpContext context) => null;
    }

    public static class NamedFluentRouteConfiguratorExtensions
    {
        public static IFluentRouteBuilder ToView(this INamedFluentRouteConfigurator self, string viewName)
        {
            return self.To(GetViewResult);

            ViewResult GetViewResult(HttpContext context)
            {
                IViewDataProvider dataProvider = context.RequestServices.GetService<IViewDataProvider>() ?? new NullViewDataProvider();
                ViewResult viewResult = new ViewResult();
                viewResult.ViewName = viewName;
                viewResult.ViewData = dataProvider.ViewData(viewName, context);
                viewResult.TempData = dataProvider.TempData(viewName, context);
                return viewResult;
            }
        }
    }
}
