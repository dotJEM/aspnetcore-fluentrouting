using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DotJEM.AspNetCore.FluentRouting.Invoker
{
    public class LambdaActionContext : ActionContext
    {
        public IList<IValueProviderFactory> ValueProviderFactories { get; set; }

        public LambdaActionContext(ActionContext context, IList<IValueProviderFactory> valueFactories)
            : base(context)
        {
            ValueProviderFactories = valueFactories;
        }

    }
}