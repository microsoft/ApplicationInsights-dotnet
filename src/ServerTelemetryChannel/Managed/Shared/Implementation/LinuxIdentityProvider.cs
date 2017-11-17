namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Shared.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;    
    using System.Text;
    using System.Threading.Tasks;

    internal class LinuxIdentityProvider : IIdentityProvider
    {
        string IIdentityProvider.GetName()
        {
            return Environment.GetEnvironmentVariable("USER");
        }
    }
}
