namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Shared.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class NonWindowsIdentityProvider : IIdentityProvider
    {
        private IDictionary environment;

        public NonWindowsIdentityProvider(IDictionary environment)
        {
            this.environment = environment;
        }

        string IIdentityProvider.GetName()
        {
            // This variable is not guaranteed to be present. eg: in Docker containers.
            if (this.environment["USER"] != null)
            {
                return this.environment["USER"].ToString();
            }            
            else
            {
                return string.Empty;
            }
        }
    }
}
