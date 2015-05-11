namespace Microsoft.ApplicationInsights.AspNet.ContextInitializers
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// A telemetry context initializer that populates device context role instance.
    /// </summary>
    public class DomainNameRoleInstanceContextInitializer : IContextInitializer
    {
        private string roleInstanceName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainNameRoleInstanceContextInitializer"/> class.
        /// </summary>
        public void Initialize(TelemetryContext context)
        {
            if (context == null)
            {
                // TODO: add diagnostics
            }

            if (string.IsNullOrEmpty(context.Device.RoleInstance))
            {
                var name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, this.GetMachineName);
                context.Device.RoleInstance = name;
            }
        }

        private string GetMachineName()
        {
            string hostName = Dns.GetHostName();

            // Issue #61: For dnxcore machine name does not have domain name like in full framework 
#if !dnxcore50
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            if (!hostName.EndsWith(domainName, StringComparison.OrdinalIgnoreCase))
            {
                hostName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", hostName, domainName);
            }
#endif
            return hostName;
        }
    }
}