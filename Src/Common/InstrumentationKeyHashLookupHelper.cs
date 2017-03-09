namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Collections.Concurrent;
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
        internal static string GetInstrumentationKeyHash(string instrumentationKey)
        {
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            string hash;
            if (!knownIKeyHashes.TryGetValue(instrumentationKey, out hash))
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(instrumentationKey.ToLowerInvariant()));
                    hash = Convert.ToBase64String(hashBytes);
                }

                // Simplistic cleanup to guard against this becoming a memory hog.
                if (knownIKeyHashes.Count >= MAXSIZE)
                {
                    knownIKeyHashes.Clear();
                }

                knownIKeyHashes[instrumentationKey] = hash;
            }

            return hash;
        }
    }
}
