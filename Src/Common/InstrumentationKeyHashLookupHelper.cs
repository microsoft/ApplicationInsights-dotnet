namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// A store for instrumentation key hashes. This is order so as to optimize the computation of hashes.
    /// </summary>
    internal static class InstrumentationKeyHashLookupHelper
    {
        /// <summary>
        /// Max number of component hashes to cache.
        /// </summary>
        private const int MAXSIZE = 100;

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
                throw new ArgumentNullException("instrumentationKey");
            }

            string hash;
            var found = knownIKeyHashes.TryGetValue(instrumentationKey, out hash);

            if (!found)
            {
                // Simplistic cleanup to guard against this becoming a memory hog.
                if (knownIKeyHashes.Keys.Count >= MAXSIZE)
                {
                    knownIKeyHashes.Clear();
                }

                hash = GenerateEncodedSHA256Hash(instrumentationKey.ToLowerInvariant());
                knownIKeyHashes[instrumentationKey] = hash;
            }

            return hash;
        }

        /// <summary>
        /// Computes the SHA256 hash for a given value and returns it in the form of a base64 encoded string.
        /// </summary>
        /// <param name="value">Value for which the hash is to be computed.</param>
        /// <returns>Base64 encoded hash string.</returns>
        private static string GenerateEncodedSHA256Hash(string value)
        {
            using (var sha256 = SHA256Managed.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
