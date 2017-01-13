namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation
{
    using System;
    using System.Globalization;
    using System.Runtime.Caching;
    using Microsoft.ApplicationInsights.DataContracts;    

    internal sealed class CacheBasedOperationHolder : IDisposable
    {
        /// <summary>
        /// The memory cache instance used to hold items. MemoryCache.Default is not used as it is shared 
        /// across application and can potentially collide with customer application.
        /// </summary>
        private readonly MemoryCache memoryCache;

        /// <summary>
        /// The cache item policy which identifies the expiration time.
        /// </summary>
        private readonly CacheItemPolicy cacheItemPolicy;
        
        public CacheBasedOperationHolder(string cacheName, long expirationInMilliSecs)
        {
            this.cacheItemPolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMilliseconds(expirationInMilliSecs) };
            this.memoryCache = new MemoryCache(cacheName);
        }

        public Tuple<DependencyTelemetry, bool> Get(long id)
        {            
            Tuple<DependencyTelemetry, bool> result = null;
            var cacheItem = this.memoryCache.GetCacheItem(id.ToString(CultureInfo.InvariantCulture));
            if (cacheItem != null)
            {
                result = (Tuple<DependencyTelemetry, bool>)cacheItem.Value;
            }

            return result;
        }

        public bool Remove(long id)
        {            
            return this.memoryCache.Remove(id.ToString(CultureInfo.InvariantCulture)) != null;
        }

        public void Store(long id, Tuple<DependencyTelemetry, bool> telemetryTuple)
        {
            if (telemetryTuple == null)
            {
                throw new ArgumentNullException("telemetryTuple");
            }

            // it might be possible to optimize by preventing the long to string conversion
            this.memoryCache.Set(id.ToString(CultureInfo.InvariantCulture), telemetryTuple, this.cacheItemPolicy);
        }

        public void Dispose()
        {
            this.memoryCache.Dispose();
        }
    }
}
