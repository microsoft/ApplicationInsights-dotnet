namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Caching;
    using System.Threading;        
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// The wrapper for MemoryCache.
    /// </summary>
    /// <typeparam name="TValue">Type of items to store in the cache.</typeparam>
    internal class CacheProvider<TValue> : ICacheProvider<TValue>
    {
        /// <summary>
        /// The memory cache instance used to hold items. MemoryCache.Default is not used as it is shared 
        /// across application and can potentially collide with customer application.
        /// </summary>
        private readonly ObjectCache memoryCache = new MemoryCache("aidependencymodule");

        /// <summary>
        /// The cache item policy which identifies the expiration time.
        /// </summary>
        private readonly CacheItemPolicy cacheItemPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheProvider{TValue}" /> class.
        /// </summary>
        /// <param name="expirationMilliseconds">Time in milliseconds for an item to expire in the cache.</param>
        public CacheProvider(int expirationMilliseconds)
        {            
            this.cacheItemPolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMilliseconds(expirationMilliseconds) };
        }

        /// <summary>
        /// Checks whether the cache entry already exists in the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>true if the cache contains a cache entry with the same key value as key; otherwise, false.</returns>
        public bool Contains(string key)
        {
            return this.memoryCache.Contains(key);
        }

        /// <summary>
        /// Gets the specified cache entry from the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>A reference to the cache entry identified by key if the entry exists; otherwise, null.</returns>
        public TValue Get(string key)
        {
            TValue result = default(TValue);
            var cacheItem = this.memoryCache.GetCacheItem(key);
            if (cacheItem != null)
            {
                result = (TValue)cacheItem.Value;
            }

            return result;
        }

        /// <summary>
        /// Inserts a cache entry into the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <param name="value">The object to insert.</param>
        public void Set(string key, TValue value)
        {
            this.memoryCache.Set(key, value, this.cacheItemPolicy);
        }

        /// <summary>
        /// Removes the cache entry from the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>True if the element is successfully found and removed; otherwise, false. This method returns false if key is not found.</returns>
        public bool Remove(string key)
        {
            return this.memoryCache.Remove(key) != null;
        }
    }              
}
