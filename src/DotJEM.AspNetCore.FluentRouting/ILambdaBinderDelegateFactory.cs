using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotJEM.AspNetCore.FluentRouting.Invoker;
using DotJEM.AspNetCore.FluentRouting.Routing.Lambdas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DotJEM.AspNetCore.FluentRouting
{
    public delegate Task LambdaBinderDelegate(LambdaActionContext context, Dictionary<string, object> arguments);

    public interface ILambdaBinderDelegateFactory
    {
        LambdaBinderDelegate CreateBinderDelegate(LambdaDescriptor descriptor);
    }

    public class LambdaBinderDelegateFactory : ILambdaBinderDelegateFactory
    {
        private readonly ParameterBinder parameterBinder;
        private readonly IModelBinderFactory modelBinderFactory;
        private readonly IModelMetadataProvider modelMetadataProvider;

        public LambdaBinderDelegateFactory(ParameterBinder parameterBinder, IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider)
        {
            this.parameterBinder = parameterBinder;
            this.modelBinderFactory = modelBinderFactory;
            this.modelMetadataProvider = modelMetadataProvider;
        }

        public LambdaBinderDelegate CreateBinderDelegate(LambdaDescriptor descriptor)
        {
            if (modelMetadataProvider == null) throw new ArgumentNullException(nameof(modelMetadataProvider));
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            BindingInfo[] parameterBindingInfo = GetParameterBindingInfo(descriptor);
            if (parameterBindingInfo == null)
                return null;

            return Bind;

            async Task Bind(LambdaActionContext context, Dictionary<string, object> arguments)
            {
                CompositeValueProvider valueProvider = await CompositeValueProvider.CreateAsync(context, context.ValueProviderFactories);
                IList<ParameterDescriptor> parameters = descriptor.Parameters;

                for (int i = 0; i < parameters.Count; i++)
                {
                    ParameterDescriptor parameter = parameters[i];
                    //TODO: Special case, perhaps a special binder would be more suited here?
                    if (parameter.ParameterType == typeof(HttpContext))
                    {
                        arguments[parameter.Name] = context.HttpContext;
                        continue;
                    }

                    BindingInfo binding = parameterBindingInfo[i];

                    ModelBindingResult result = await parameterBinder
                        .BindModelAsync(context, binding.ModelBinder, valueProvider, parameter, binding.ModelMetadata, null);

                    if (result.IsModelSet)
                        arguments[parameter.Name] = result.Model;
                }
            }
        }

        private BindingInfo[] GetParameterBindingInfo(LambdaDescriptor descriptor)
        {
            IList<ParameterDescriptor> parameters = descriptor.Parameters;
            if (parameters.Count == 0)
                return null;

            BindingInfo[] parameterBindingInfo = new BindingInfo[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterDescriptor parameter = parameters[i];
                //TODO: Special case, perhaps a special binder would be more suited here?
                if (parameter.ParameterType == typeof(HttpContext))
                {
                    //Note: If we hit a HttpContext parameter, we don't need meta data etc.
                    continue;
                }

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
            public IModelBinder ModelBinder { get; }
            public ModelMetadata ModelMetadata { get; }

            public BindingInfo(IModelBinder modelBinder, ModelMetadata modelMetadata)
            {
                ModelBinder = modelBinder;
                ModelMetadata = modelMetadata;
            }
        }
    }
}