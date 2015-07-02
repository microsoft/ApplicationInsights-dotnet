namespace Microsoft.ApplicationInsights
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Remoting.Metadata.W3cXsd2001;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Various utilities.
    /// </summary>
    internal static partial class Utils
    {
        /// <summary>
        /// The relative path to the cache for our application data.
        /// </summary>
        private static readonly string[] RelativeFolderPath = new string[] { "Microsoft", "ApplicationInsights", "Cache" };

        /// <summary>
        /// Gets the input string as a SHA256 Base64 encoded string.
        /// </summary>
        /// <param name="input">The input to hash.</param>
        /// <param name="isCaseSensitive">If set to <c>false</c> the function will produce the same value for any casing of input.</param>
        /// <returns>The hashed value.</returns>
        public static string GetHashedId(string input, bool isCaseSensitive = false)
        {
            // for nulls, return an empty string, else hash.
            if (input == null)
            {
                return string.Empty;
            }

            using (SHA256 hasher = new SHA256Cng())
            {
                string temp = input;
                if (isCaseSensitive == false)
                {
                    temp = input.ToUpperInvariant();
                }

                byte[] buffer = hasher.ComputeHash(Encoding.UTF8.GetBytes(temp));
                return new SoapBase64Binary(buffer).ToString();
            }
        }        
    }
}
