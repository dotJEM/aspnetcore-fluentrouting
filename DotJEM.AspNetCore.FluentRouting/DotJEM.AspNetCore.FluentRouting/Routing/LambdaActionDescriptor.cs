using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DotJEM.AspNetCore.FluentRouter.Routing
{
    public class LambdaActionDescriptor : ActionDescriptor
    {
        public Delegate Delegate { get; }

        public LambdaActionDescriptor(Delegate @delegate)
        {
            Delegate = @delegate;
            Parameters = CreateParameterDescriptors(@delegate.Method.GetParameters()).ToList();

            FilterDescriptors = new List<FilterDescriptor>();
            ActionConstraints = new List<IActionConstraintMetadata>();

            //Note: Functions cannot have properties.
            BoundProperties = null;

        }

        private static IEnumerable<ParameterDescriptor> CreateParameterDescriptors(ParameterInfo[] parameters)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                (Type type, BindingInfo binding) = Resolve(parameter);
                yield return new ParameterDescriptor
                {
                    Name = parameter.Name,
                    ParameterType = type,
                    BindingInfo = binding
                };
            }
        }

        private static (Type, BindingInfo) Resolve(ParameterInfo parameter)
        {
            //TODO: Move out and cache
            Type t = parameter.ParameterType;
            if (t.IsGenericType)
            {
                Type gt = t.GetGenericTypeDefinition();
                if (gt == typeof(FromHeader<>))
                {
                    BindingInfo binding = new BindingInfo();
                    binding.BindingSource = BindingSource.Header;
                    return (t.GetGenericArguments().Single(), binding);
                }
                if (gt == typeof(FromServices<>))
                {
                    BindingInfo binding = new BindingInfo();
                    binding.BindingSource = BindingSource.Services;
                    return (t.GetGenericArguments().Single(), binding);
                }
                if (gt == typeof(FromRoute<>))
                {
                    BindingInfo binding = new BindingInfo();
                    binding.BindingSource = BindingSource.Path;
                    return (t.GetGenericArguments().Single(), binding);
                }
                if (gt == typeof(FromQuery<>))
                {
                    BindingInfo binding = new BindingInfo();
                    binding.BindingSource = BindingSource.Query;
                    return (t.GetGenericArguments().Single(), binding);
                }
                if (gt == typeof(FromForm<>))
                {
                    BindingInfo binding = new BindingInfo();
                    binding.BindingSource = BindingSource.Form;
                    return (t.GetGenericArguments().Single(), binding);
                }
                if (gt == typeof(FromBody<>))
                {
                    BindingInfo binding = new BindingInfo();
                    binding.BindingSource = BindingSource.Body;
                    return (t.GetGenericArguments().Single(), binding);
                }
                if (gt == typeof(FromUri<>))
                {
                    BindingInfo binding = new BindingInfo();
                    binding.BindingSource = BindingSource.Custom;
                    return (t.GetGenericArguments().Single(), binding);
                }
            }
            return (t, null);
        }

    }

    public interface IBindingSourceParameter { }

    public abstract class BindingSourceParameter<T>
    {
        public T Value { get; }

        protected BindingSourceParameter(T value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            BindingSourceParameter<T> parameter = obj as BindingSourceParameter<T>;
            if(parameter != null)
                return EqualityComparer<T>.Default.Equals(Value, parameter.Value);

            if(obj is T)
                return EqualityComparer<T>.Default.Equals(Value, (T)obj);

            return false;
        }

        public override int GetHashCode()
        {
            return -1937169414 + EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value?.ToString();
        }

        public static implicit operator T(BindingSourceParameter<T> parameter) => parameter.Value;
    }

    //TODO: Can we get these to work in standard MVC controllers as well? -> https://docs.microsoft.com/en-us/aspnet/core/mvc/advanced/custom-model-binding
    public class FromHeader<T> : BindingSourceParameter<T>{
        public FromHeader(T value) : base(value)
        {
        }
        public static implicit operator FromHeader<T>(T value) => new FromHeader<T>(value);
    }
    public class FromServices<T> : BindingSourceParameter<T>{
        public FromServices(T value) : base(value)
        {
        }
        public static implicit operator FromServices<T>(T value) => new FromServices<T>(value);
    }
    public class FromRoute<T> : BindingSourceParameter<T>{
        public FromRoute(T value) : base(value)
        {
        }
        public static implicit operator FromRoute<T>(T value) => new FromRoute<T>(value);
    }
    public class FromQuery<T> : BindingSourceParameter<T>{
        public FromQuery(T value) : base(value)
        {
        }
        public static implicit operator FromQuery<T>(T value) => new FromQuery<T>(value);
    }
    public class FromForm<T> : BindingSourceParameter<T>{
        public FromForm(T value) : base(value)
        {
        }
        public static implicit operator FromForm<T>(T value) => new FromForm<T>(value);
    }
    public class FromBody<T> : BindingSourceParameter<T>{
        public FromBody(T value) : base(value)
        {
        }
        public static implicit operator FromBody<T>(T value) => new FromBody<T>(value);
    }
    public class FromUri<T> : BindingSourceParameter<T>{
        public FromUri(T value) : base(value)
        {
            
        }
        public static implicit operator FromUri<T>(T value) => new FromUri<T>(value);
    }
}