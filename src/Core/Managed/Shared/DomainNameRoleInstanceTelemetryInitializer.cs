namespace Microsoft.ApplicationInsights
{   
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;    
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

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
            CoreEventSource.Log.TelemetryInitializerLoaded(this.GetType().FullName);
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

            if (string.IsNullOrEmpty(telemetry.Context.Internal.NodeName))
            {
                var name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, this.GetMachineName);
                telemetry.Context.Internal.NodeName = name;
            }
        }

        private string GetMachineName()
        {
            string hostName = string.Empty;

#if !CORE_PCL
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            hostName = Dns.GetHostName();

            if (!hostName.EndsWith(domainName, StringComparison.OrdinalIgnoreCase))
            {
                hostName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", hostName, domainName);
            }
#endif

            return hostName;
        }
    }
}
