using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotJEM.AspNetCore.FluentRouter.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DotJEM.AspNetCore.FluentRouter
{
    public delegate Task FunctionBinderDelegate(FluentActionContext context, Dictionary<string, object> arguments);

    public class FuncBinderDelegateProvider
    {
        //TODO: Service.
        public static FunctionBinderDelegate CreateBinderDelegate(
            ParameterBinder parameterBinder, 
            IModelBinderFactory modelBinderFactory, 
            IModelMetadataProvider modelMetadataProvider, 
            LambdaActionDescriptor descriptor)
        {
            if (parameterBinder == null) throw new ArgumentNullException(nameof(parameterBinder));
            if (modelMetadataProvider == null) throw new ArgumentNullException(nameof(modelMetadataProvider));
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            var parameterBindingInfo = GetParameterBindingInfo(modelBinderFactory, modelMetadataProvider, descriptor);
            if (parameterBindingInfo == null)
                return null;

            return Bind;

            async Task Bind(FluentActionContext context, Dictionary<string, object> arguments)
            {
                CompositeValueProvider valueProvider = await CompositeValueProvider.CreateAsync(context, context.ValueProviderFactories);
                IList<ParameterDescriptor> parameters = descriptor.Parameters;

                for (int i = 0; i < parameters.Count; i++)
                {
                    ParameterDescriptor parameter = parameters[i];
                    BindingInfo binding = parameterBindingInfo[i];

                    ModelBindingResult result = await parameterBinder
                        .BindModelAsync(context, binding.ModelBinder, valueProvider, parameter, binding.ModelMetadata, null);

                    if (result.IsModelSet)
                        arguments[parameter.Name] = result.Model;
                }
            }
        }

        private static BindingInfo[] GetParameterBindingInfo(IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider, LambdaActionDescriptor descriptor)
        {
            IList<ParameterDescriptor> parameters = descriptor.Parameters;
            if (parameters.Count == 0)
                return null;

            BindingInfo[] parameterBindingInfo = new BindingInfo[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterDescriptor parameter = parameters[i];
                ModelMetadata metadata = modelMetadataProvider.GetMetadataForType(parameter.ParameterType);
                IModelBinder binder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
                {
                    BindingInfo = parameter.BindingInfo,
                    Metadata = metadata,
                    CacheToken = parameter
                });

                parameterBindingInfo[i] = new BindingInfo(binder, metadata);
            }

            return parameterBindingInfo;
        }

        private struct BindingInfo
        {
            public BindingInfo(IModelBinder modelBinder, ModelMetadata modelMetadata)
            {
                ModelBinder = modelBinder;
                ModelMetadata = modelMetadata;
            }

            public IModelBinder ModelBinder { get; }

            public ModelMetadata ModelMetadata { get; }
        }
    }

}