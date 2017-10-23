using System;
using DotJEM.AspNetCore.FluentRouting.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DotJEM.AspNetCore.FluentRouting.Views
{
    public static class NamedFluentRouteConfiguratorExtensions
    {
        public static IFluentRouteBuilder ToView(this INamedFluentRouteConfigurator self, string viewName)
        {
            ViewResult GetViewResult(HttpContext context)
            {
                ViewResult viewResult = new ViewResult();
                viewResult.ViewName = viewName;
                ViewDataDictionary viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
                viewData.Model = new { };

                viewResult.ViewData = viewData;
                //ITempDataDictionary tempData = null;//this.TempData;
                viewResult.TempData = null;
                return viewResult;
            }
            return self.To(GetViewResult);
        }
    }
}
