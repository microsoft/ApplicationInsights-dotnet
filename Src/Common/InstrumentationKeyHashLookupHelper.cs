namespace Microsoft.ApplicationInsights.Common
{
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// A store for instrumentation key hashes. This is order so as to optimize the computation of hashes.
    /// </summary>
    public static class InstrumentationKeyHashLookupHelper
    {
        private static ConcurrentDictionary<string, string> knownIKeyHashes = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Retrieves the hash for a given instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key string.</param>
        /// <returns>SHA256 hash for the instrumentation key.</returns>
        public static string GetInstrumentationKeyHash(string instrumentationKey)
        {
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                return null;
            }

            if (!knownIKeyHashes.ContainsKey(instrumentationKey))
            {
                knownIKeyHashes[instrumentationKey] = GenerateSHA256Hash(instrumentationKey);
            }

            return knownIKeyHashes[instrumentationKey];
        }

        /// <summary>
        /// Computes the SHA256 hash for a given value.
        /// </summary>
        /// <param name="value">Value for which the hash is to be computed.</param>
        /// <returns>Hash string.</returns>
        private static string GenerateSHA256Hash(string value)
        {
            string hashString = string.Empty;

            var sha256 = SHA256Managed.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));

            foreach (byte x in hash)
            {
                hashString += string.Format(CultureInfo.InvariantCulture, "{0:x2}", x);
            }

            return hashString;
        }
    }
}
