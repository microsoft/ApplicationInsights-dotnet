namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using OpenTelemetry;

    /// <summary>
    /// Activity processor that populates client IP address for the current request.
    /// </summary>
    internal class ClientIpHeaderActivityProcessor : BaseProcessor<Activity>
    {
        private const string HeaderNameDefault = "X-Forwarded-For";
        private readonly char[] headerValuesSeparatorDefault = new char[] { ',' };

        private readonly ICollection<string> headerNames;
        private char[] headerValueSeparators;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientIpHeaderActivityProcessor"/> class.
        /// </summary>
        public ClientIpHeaderActivityProcessor()
        {
            this.headerNames = new List<string>();
            this.HeaderNames.Add(HeaderNameDefault);
            this.UseFirstIp = true;
            this.headerValueSeparators = this.headerValuesSeparatorDefault;
        }

        /// <summary>
        /// Gets a list of request header names that is used to check client id.
        /// </summary>
        public ICollection<string> HeaderNames
        {
            get
            {
                return this.headerNames;
            }
        }

        /// <summary>
        /// Gets or sets a header values separator.
        /// </summary>
        public string HeaderValueSeparators
        {
            get
            {
                return string.Concat(this.headerValueSeparators);
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.headerValueSeparators = value.ToCharArray();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the first or the last IP should be used from the lists of IPs in the header.
        /// </summary>
        public bool UseFirstIp { get; set; }

        /// <summary>
        /// Called when an activity ends. Sets the client IP address tag.
        /// </summary>
        /// <param name="activity">The activity that ended.</param>
        public override void OnEnd(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            var context = HttpContext.Current;
            if (context == null)
            {
                return;
            }

            // Only process if client IP is not already set
            var existingIp = activity.GetTagItem("client.address") ?? activity.GetTagItem("microsoft.client.ip");
            if (existingIp == null || string.IsNullOrEmpty(existingIp?.ToString()))
            {
                string resultIp = null;
                foreach (var clientIpHeaderName in this.HeaderNames)
                {
                    // Try Unvalidated first, fall back to regular Headers for test environments
                    var clientIpsFromHeader = context.Request.UnvalidatedGetHeader(clientIpHeaderName);
                    if (string.IsNullOrWhiteSpace(clientIpsFromHeader))
                    {
                        clientIpsFromHeader = context.Request.Headers[clientIpHeaderName];
                    }

                    if (!string.IsNullOrWhiteSpace(clientIpsFromHeader))
                    {
                        WebEventSource.Log.WebLocationIdHeaderFound(clientIpHeaderName);

                        string ip = this.GetIpFromHeader(clientIpsFromHeader);
                        if (TryParseIpWithPort(ip, out var ipAddressString))
                        {
                            resultIp = ipAddressString;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(resultIp))
                {
                    resultIp = context.Request.GetUserHostAddress();
                }

                if (!string.IsNullOrEmpty(resultIp))
                {
                    // Set as OpenTelemetry semantic convention for client address
                    activity.SetTag("client.address", resultIp);
                    WebEventSource.Log.WebLocationIdSet(resultIp);
                }
            }

            base.OnEnd(activity);
        }

        private static bool TryParseIpWithPort(string input, out string ipAddressString)
        {
            Uri uri;
            ipAddressString = null;

            if (Uri.TryCreate($"tcp://{input}", UriKind.Absolute, out uri) ||
                Uri.TryCreate($"tcp://[{input}]", UriKind.Absolute, out uri))
            {
                if (IPAddress.TryParse(uri.Host, out var ip))
                {
                    ipAddressString = new IPEndPoint(ip, uri.Port < 0 ? 0 : uri.Port).Address.ToString();
                    return true;
                }
            }

            return false;
        }

        private string GetIpFromHeader(string clientIpsFromHeader)
        {
            var ips = clientIpsFromHeader.Split(this.headerValueSeparators, StringSplitOptions.RemoveEmptyEntries);
            return this.UseFirstIp ? ips[0].Trim() : ips[ips.Length - 1].Trim();
        }
    }
}
