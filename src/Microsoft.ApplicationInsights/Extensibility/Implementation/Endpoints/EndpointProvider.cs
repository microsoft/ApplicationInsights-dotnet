namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights.Common.Extensions;

    /// <summary>
    /// This class encapsulates parsing a connection string and returning an Endpoint's Uri.
    /// </summary>
    internal class EndpointProvider : IEndpointProvider
    {
        /// <summary>
        /// Maximum allowed length connection string.
        /// </summary>
        /// <remarks>
        /// Currently 8 accepted keywords (~200 characters).
        /// Assuming 200 characters per value (~1600 characters). 
        /// Total theoretical max length = (1600 + 200) = 1800.
        /// Setting an over-exaggerated max length to protect against malicious injections (2^12 = 4096).
        /// </remarks>
        internal const int ConnectionStringMaxLength = 4096;

        private static readonly char[] TrimPeriod = new char[] { '.' };

        private string connectionString;
        private Dictionary<string, string> connectionStringParsed = new Dictionary<string, string>(0);

        /// <summary>
        /// Gets or sets the connection string. 
        /// Connection String will be in the format: "key1=value1;key2=value2;key3=value3".
        /// Keywords are: InstrumentationKey, Authorization, Location, EndpointSuffix.
        /// Explicit Endpoint Keywords are: IngestionEndpoint, LiveEndpoint, ProfilerEndpoint, SnapshotEndpoint.
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return this.connectionString;
            }

            set
            {
                if (value != null && value.Length > ConnectionStringMaxLength)
                {
                    // TODO: LOG TO ETW
                    throw new ArgumentOutOfRangeException($"Values greater than {ConnectionStringMaxLength} characters are not allowed.", nameof(this.ConnectionString));
                }

                this.connectionString = value ?? throw new ArgumentNullException(nameof(this.ConnectionString));
                this.connectionStringParsed = ParseConnectionString(value);
            }
        }

        /// <summary>
        /// Will evaluate connection string and return the requested endpoint.
        /// </summary>
        /// <param name="endpointName">Specify which endpoint you want.</param>
        /// <returns>Returns a <see cref="Uri" /> for the requested endpoint.</returns>
        public Uri GetEndpoint(EndpointName endpointName)
        {
            // 1. check for explicit endpoint (location is ignored)
            // 2. check for endpoint suffix (location is optional)
            // 3. use classic endpoint (location is ignored)

            var endpointMeta = EndpointMetaAttribute.GetAttribute(endpointName);

            try
            {
                if (this.connectionStringParsed.TryGetValue(endpointMeta.ExplicitName, out string explicitEndpoint))
                {
                    return new Uri(explicitEndpoint);
                }
                else if (this.connectionStringParsed.TryGetValue("EndpointSuffix", out string endpointSuffix))
                {
                    return BuildUri(
                        prefix: endpointMeta.EndpointPrefix,
                        suffix: endpointSuffix,
                        location: this.GetLocation());
                }
                else
                {
                    return new Uri(endpointMeta.Default);
                }
            }
            catch (Exception ex)
            {
                // TODO: LOG TO ETW
                return new Uri(endpointMeta.Default);
            }
        }

        /// <summary>
        /// Will evaluate connection string and return the requested instrumentation key.
        /// </summary>
        /// <returns>Returns the instrumentation key from the connection string.</returns>
        public bool TryGetInstrumentationKey(out string value)
        {
            if (this.connectionStringParsed.TryGetValue("InstrumentationKey", out value))
            {
                return !string.IsNullOrEmpty(value);
            }

            return false;
        }

        /// <summary>
        /// Parse a string connection string and return a Dictionary.
        /// </summary>
        /// <remarks>Example: "key1=value1;key2=value2;key3=value3".</remarks>
        /// <returns>A dictionary parsed from the input connection string.</returns>
        /// <exception cref="ArgumentException">Thrown if there are duplicate keys.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown if the input string is in the wrong format.</exception>
        internal static Dictionary<string, string> ParseConnectionString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new Dictionary<string, string>(0);
            }

            try
            {
                return value
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Split('='))
                    .ToDictionary(split => split[0], split => split[1], StringComparer.OrdinalIgnoreCase);
            }
            catch (ArgumentException ex) when (ex.Message.StartsWith("An item with the same key has already been added.", StringComparison.Ordinal))
            {
                // TODO: LOG TO ETW
                throw new ConnectionStringDuplicateKeyException("The Connection String has duplicate keys.", ex);
            }
            catch (IndexOutOfRangeException ex) when (ex.Message.StartsWith("Index was outside the bounds of the array.", StringComparison.Ordinal))
            {
                // TODO: LOG TO ETW
                throw new ConnectionStringInvalidDelimiterException("The Connection String has invalid delimiters. Expected: 'key1=value1;key2=value2;key3=value3'", ex);
            }
        }

        /// <summary>
        /// Construct a Uri from the possible parts.
        /// Will also attempt to sanitize user input.
        /// </summary>
        /// <remarks>
        /// Format: "location.prefix.suffix".
        /// Example: "https:// westus2.dc.applicationinsights.azure.cn/".
        /// </remarks>
        /// <returns>Returns a <see cref="Uri"/> built from the inputs.</returns>
        internal static Uri BuildUri(string prefix, string suffix, string location = null)
        {
            // Location and Host are user input fields and need to be checked for extra periods.

            var uriString = string.Concat("https://"
                + (string.IsNullOrEmpty(location) ? string.Empty : (location.TrimEnd(TrimPeriod) + "."))
                + prefix 
                + "." + suffix.TrimStart(TrimPeriod));

            return new Uri(uriString);
        }

        private string GetLocation() => this.connectionStringParsed.TryGetValue("Location", out string location) ? location : null;
    }
}
