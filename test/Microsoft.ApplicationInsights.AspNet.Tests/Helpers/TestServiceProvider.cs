namespace Microsoft.ApplicationInsights.AspNet.Tests.Helpers
{
    using System;
    using System.Reflection;
    using System.Collections.Generic;

    public class TestServiceProvider : IServiceProvider
    {
        private IList<object> knownInstances;

        public TestServiceProvider(IList<object> knownInstances = null)
        {
            this.knownInstances = knownInstances;
        }

        public object GetService(Type serviceType)
        {
            if (this.knownInstances != null)
            {
                foreach (object instance in this.knownInstances)
                {
                    var instanceType = instance.GetType();

                    if (instanceType == serviceType)
                    {
                        return instance;
                    }

                    foreach (var interfaceType in instanceType.GetTypeInfo().ImplementedInterfaces)
                    {
                        if (interfaceType == serviceType)
                        {
                            return instance;
                        }
                    }
                }
            }

            return null;
        }
    }
}