namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;
    using Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;    

    /// <summary>
    /// A telemetry initializer that populates cloud context role instance.
    /// </summary>
    public class DomainNameRoleInstanceTelemetryInitializer : TelemetryInitializerBase
    {
        private string roleInstanceName;

        public DomainNameRoleInstanceTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
            {
                var name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, this.GetMachineName);
                telemetry.Context.Cloud.RoleInstance = name;
            }

            if (string.IsNullOrEmpty(telemetry.Context.GetInternalContext().NodeName))
            {
                var name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, this.GetMachineName);
                telemetry.Context.GetInternalContext().NodeName = name;
            }
        }

        private string GetMachineName()
        {
            string hostName = Dns.GetHostName();

            // Issue #61: For dnxcore machine name does not have domain name like in full framework 
#if !NETSTANDARD1_6
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