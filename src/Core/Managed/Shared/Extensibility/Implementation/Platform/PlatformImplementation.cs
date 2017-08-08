#if !NETSTANDARD1_3 // netstandard1.3 has it's own implementation
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.NetworkInformation;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// The .NET 4.0, 4.5 and 4.6 implementation of the <see cref="IPlatform"/> interface.
    /// </summary>
    internal sealed class PlatformImplementation : PlatformImplementationBase
    {
        /// <summary>
        /// The directory where the configuration file might be found.
        /// </summary>
        protected override string ConfigurationXmlDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        /// <summary>
        /// Returns the machine name.
        /// </summary>
        /// <returns>The machine name.</returns>
        protected override string GetHostNameCore()
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
#endif