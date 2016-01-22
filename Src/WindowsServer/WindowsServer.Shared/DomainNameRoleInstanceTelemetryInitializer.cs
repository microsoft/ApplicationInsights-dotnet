namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;
    
    /// <summary>
    /// A telemetry context initializer that populates device context role instance name.
    /// </summary>
    public class DomainNameRoleInstanceTelemetryInitializer : ITelemetryInitializer
    {
        private string roleInstanceName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainNameRoleInstanceTelemetryInitializer" /> class.
        /// </summary>
        public DomainNameRoleInstanceTelemetryInitializer()
        {
            WindowsServerEventSource.Log.TelemetryInitializerLoaded(this.GetType().FullName);
        }

        /// <summary>
        /// Initializes <see cref="ITelemetry" /> device context.
        /// </summary>
        /// <param name="telemetry">The telemetry to initialize.</param>
        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
            {
                var name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, this.GetMachineName);
                telemetry.Context.Cloud.RoleInstance = name;
            }
        }

        private string GetMachineName()
        {
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string hostName = Dns.GetHostName();

            if (!hostName.EndsWith(domainName, StringComparison.OrdinalIgnoreCase))
            {
                hostName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", hostName, domainName);
            }

            return hostName;
        }
    }
}
