namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerformanceCollector
{
    using System;
    using System.Runtime.Caching;
    using System.Text;

    /// <summary>
    /// Class to contain the one cache for all Gauges.
    /// </summary>
    internal class CacheHelper : ICachedEnvironmentVariableAccess
    {
        /// <summary>
        /// Only instance of CacheHelper.
        /// </summary>
        private static readonly CacheHelper CacheHelperInstance = new CacheHelper();

        /// <summary>
        /// Prevents a default instance of the <see cref="CacheHelper"/> class from being created.
        /// </summary>
        private CacheHelper()
        {
        }

        /// <summary>
        /// Gets the only instance of CacheHelper.
        /// </summary>
        public static CacheHelper Instance
        {
            get
            {
                return CacheHelperInstance;
            }
        }

        /// <summary>
        /// Search for the value of a given performance counter in a JSON.
        /// </summary>
        /// <param name="performanceCounterName"> The name of the performance counter.</param>
        /// <param name="json"> String containing the JSON.</param>
        /// <returns> Value of the performance counter.</returns>
        public long PerformanceCounterValue(string performanceCounterName, string json)
        {
            if (json.IndexOf(performanceCounterName, StringComparison.OrdinalIgnoreCase) == -1)
            {
                throw new System.ArgumentException("Counter was not found.", performanceCounterName);
            }

            string jsonSubstring = json.Substring(json.IndexOf(performanceCounterName, StringComparison.OrdinalIgnoreCase), json.Length - json.IndexOf(performanceCounterName, StringComparison.OrdinalIgnoreCase));

            int startingIndex = jsonSubstring.IndexOf(" ", StringComparison.Ordinal) + 1;
            long value;
            StringBuilder valueString = new StringBuilder();

            while (char.IsDigit(jsonSubstring[startingIndex]))
            {
                valueString.Append(jsonSubstring[startingIndex]);
                startingIndex++;
            }

            if (!long.TryParse(valueString.ToString(), out value))
            {
                throw new System.InvalidCastException("The value of the counter cannot be converted to long type. Value:" + valueString);
            }

            return value;
        }

        /// <summary>
        /// Checks if a key is in the cache and if not
        /// Retrieves raw counter data from Environment Variables
        /// Cleans raw JSON for only requested counter
        /// Creates value for caching.
        /// </summary>
        /// <param name="name">Cache key and name of the counter to be selected from JSON.</param>
        /// <param name="environmentVariable">Identifier of the environment variable.</param>
        /// <returns>Value from cache.</returns>
        public long GetCounterValue(string name, AzureWebApEnvironmentVariables environmentVariable)
        {
            if (!CacheHelper.Instance.IsInCache(name))
            {
                PerformanceCounterImplementation client = new PerformanceCounterImplementation();
                string uncachedJson = client.GetAzureWebAppEnvironmentVariables(environmentVariable);

                if (uncachedJson == null)
                {
                    return 0;
                }

                CacheHelper.Instance.SaveToCache(name, uncachedJson, DateTimeOffset.Now.AddMilliseconds(500));
            }

            string json = this.GetFromCache(name).ToString();
            long value = this.PerformanceCounterValue(name, json);

            return value;
        }

        /// <summary>
        /// Method saves an object to the cache.
        /// </summary>
        /// <param name="cacheKey"> String name of the counter value to be saved to cache.</param>
        /// /<param name="toCache">Object to be cached.</param>
        /// <param name="absoluteExpiration">DateTimeOffset until item expires from cache.</param>
        public void SaveToCache(string cacheKey, object toCache,  DateTimeOffset absoluteExpiration)
        {  
            MemoryCache.Default.Add(cacheKey, toCache, absoluteExpiration);
        }

        /// <summary>
        /// Retrieves requested item from cache.
        /// </summary>
        /// <param name="cacheKey"> Key for the retrieved object.</param>
        /// <returns> The requested item, as object type T.</returns>
        public object GetFromCache(string cacheKey) 
        {
            return MemoryCache.Default[cacheKey];
        }

        /// <summary>
        /// Method to check if a key is in a cache.
        /// </summary>
        /// <param name="cacheKey">Key to search for in cache.</param>
        /// <returns>Boolean value for whether or not a key is in the cache.</returns>
        public bool IsInCache(string cacheKey)
        {
            return MemoryCache.Default[cacheKey] != null;
        }
    }
}
