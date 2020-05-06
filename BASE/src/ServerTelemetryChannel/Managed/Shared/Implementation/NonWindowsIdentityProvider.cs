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
#if NETSTANDARD2_0
            if (Environment.UserName != null)
            {
                return Environment.UserName;
            }
#endif

            // This variable is not guaranteed to be present. eg: in Docker containers.
            if (this.environment["USER"] != null)
            {
                return this.environment["USER"].ToString();
            }

            if (this.environment["USERNAME"] != null)
            {
                return this.environment["USERNAME"].ToString();
            }

            return string.Empty;
        }
    }
}
