﻿namespace Microsoft.ApplicationInsights.AspNetCore.Implementation
{
    using System;
    using System.Threading;

    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.AspNetCore.Http;

    internal static class RoleNameContainer
    {
        private const string WebAppHostNameHeaderName = "WAS-DEFAULT-HOSTNAME";
        private const string WebAppHostNameEnvironmentVariable = "WEBSITE_HOSTNAME";

        private static string roleName = string.Empty;

        public static string RoleName
        {
            get => roleName;

            set
            {
                if (value != roleName)
                {
                    Interlocked.Exchange(ref roleName, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets suffix of website name. This must be changed when running in non public Azure region.
        /// Default value (Public Cloud):  ".azurewebsites.net"
        /// For US Gov Cloud: ".azurewebsites.us"
        /// For Azure Germany: ".azurewebsites.de".
        /// </summary>
        public static string HostNameSuffix { get; set; } = ".azurewebsites.net";

        public static void SetFromEnvironmentVariable(out bool isAzureWebApp)
        {
            try
            {
                var value = Environment.GetEnvironmentVariable(WebAppHostNameEnvironmentVariable);
                ParseAndSetRoleName(value);
                
                isAzureWebApp = string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                AspNetCoreEventSource.Instance.LogAzureAppServiceRoleNameFromHostNameHeaderInitializerWarning(ex.ToInvariantString());
                isAzureWebApp = false;
            }
        }

        public static void Set(IHeaderDictionary requestHeaders)
        {
            string headerValue = requestHeaders[WebAppHostNameHeaderName];
            ParseAndSetRoleName(headerValue);
        }

        private static void ParseAndSetRoleName(string input)
        {
            if (!string.IsNullOrEmpty(input) && input.EndsWith(HostNameSuffix, StringComparison.OrdinalIgnoreCase))
            {
                RoleName = input.Substring(0, input.LastIndexOf(HostNameSuffix, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                RoleName = input;
            }
        }
    }
}
