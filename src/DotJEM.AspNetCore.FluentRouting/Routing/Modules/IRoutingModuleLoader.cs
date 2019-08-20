using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DotJEM.AspNetCore.FluentRouting.Routing.Modules
{
    public interface IRoutingModuleLoader
    {
        IRoutingModuleLoader Load<T>();
        IRoutingModuleLoader Load(Type type);
    }

    public class RoutingModuleLoader : IRoutingModuleLoader
    {
        public IRoutingModuleLoader Load<T>()
        {
            throw new NotImplementedException();
        }

        public IRoutingModuleLoader Load(Type type)
        {
            throw new NotImplementedException();
        }
    }

    public interface IRoutingModuleDiscovery
    {
        IRoutingModuleDiscovery LoadAll();
        IRoutingModuleDiscovery LoadFrom(Assembly assembly);
    }

    public class DefaultRoutingModuleDiscovery : IRoutingModuleDiscovery
    {
        public IRoutingModuleDiscovery LoadAll()
        {
            throw new NotImplementedException();
        }

        public IRoutingModuleDiscovery LoadFrom(Assembly assembly)
        {
            IEnumerable<Type> modules = assembly.GetTypes().Where(t => typeof(IRoutingModule).IsAssignableFrom(t));

            return this;
        }
    }

    public interface IRoutingModule
    {
        
    }
}
