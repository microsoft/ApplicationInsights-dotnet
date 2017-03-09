namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// WebHeaderCollection extension methods.
    /// </summary>
    internal static class WebHeaderCollectionExtensions
    {
        /// <summary>
        /// For the given header collection, for a given header of name-value type, find the value of a particular key.
        /// </summary>
        /// <param name="headers">Header collection.</param>
        /// <param name="headerName">Name of the header in the collection.</param>
        /// <param name="keyName">Desired key of the key-value list.</param>
        /// <returns>Value against the given parameters.</returns>
        public static string GetNameValueHeaderValue(this NameValueCollection headers, string headerName, string keyName)
        {
            if (string.IsNullOrEmpty(headerName))
            {
                throw new ArgumentNullException(nameof(headerName));
            }

            if (string.IsNullOrEmpty(keyName))
            {
                throw new ArgumentNullException(nameof(keyName));
            }

            var requiredHeader = headers[headerName];

            if (requiredHeader != null)
            {
                var headerValues = requiredHeader.Split(',');
                foreach (var headerValue in headerValues)
                {
                    string keyValueString = headerValue.Trim();
                    var keyValue = keyValueString.Split('=');

                    if (keyValue.Length == 2 && keyValue[0].Trim() == keyName)
                    {
                        return keyValue[1].Trim();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// For the given header collection, sets the header value based on the name value format.
        /// </summary>
        /// <param name="headers">Header collection.</param>
        /// <param name="headerName">Name of the header that is to contain the name-value pair.</param>
        /// <param name="keyName">Name in the name value pair.</param>
        /// <param name="value">Value in the name value pair.</param>
        public static void SetNameValueHeaderValue(this NameValueCollection headers, string headerName, string keyName, string value)
        {
            if (string.IsNullOrEmpty(headerName))
            {
                throw new ArgumentNullException(nameof(headerName));
            }

            if (string.IsNullOrEmpty(keyName))
            {
                throw new ArgumentNullException(nameof(keyName));
            }
            
            var requiredHeader = headers[headerName];

            if (!string.IsNullOrEmpty(requiredHeader))
            {
                bool found = false;
                var headerValues = requiredHeader.Split(',').Select(s => s.Trim()).ToArray();
                for (int i = 0; i < headerValues.Length; i++)
                {
                    string keyValueString = headerValues[i];
                    var keyValue = keyValueString.Split('=');

                    if (keyValue.Length == 2 && keyValue[0].Trim() == keyName)
                    {
                        // Overwrite the existing thing
                        headerValues[i] = string.Format("{0}={1}", keyName, value, CultureInfo.InvariantCulture);
                        found = true;
                    }
                }

                headers[headerName] = string.Join(", ", headerValues);

                if (!found)
                {
                    headers[headerName] += string.Format(", {0}={1}", keyName, value, CultureInfo.InvariantCulture);
                }
            }
            else
            {
                // header with headerName does not exist - let's add one.
                headers[headerName] = string.Format("{0}={1}", keyName, value, CultureInfo.InvariantCulture);
            }
        }
    }
}
