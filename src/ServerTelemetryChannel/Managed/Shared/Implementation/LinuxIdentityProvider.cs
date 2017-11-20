namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Shared.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class LinuxIdentityProvider : IIdentityProvider
    {
        private IDictionary environment;

        public LinuxIdentityProvider(IDictionary environment)
        {
            this.environment = environment;
        }

        string IIdentityProvider.GetName()
        {
            return this.environment["USER"].ToString();
        }
    }
}
