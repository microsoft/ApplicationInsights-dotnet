namespace Microsoft.ApplicationInsights.AspNet.Tests.Helpers
{
    using System;
    using System.Collections.Generic;

    // TODO: REPLACE ON MOQ
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
                    if (instance.GetType() == serviceType)
                    {
                        return instance;
                    }
                }
            }

            return null;
        }
    }
}