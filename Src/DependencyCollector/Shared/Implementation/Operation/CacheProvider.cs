namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// The implementation of cache provider for Windows Phone and Windows Store as MemoryCache is not available there.
    /// </summary>
    /// <typeparam name="TValue">Type of items to store in the cache.</typeparam>
    internal sealed class CacheProvider<TValue> : ICacheProvider<TValue>
    {
        /// <summary>
        /// Reader-Writer Lock for thread safety.
        /// </summary>
        private readonly ReaderWriterLockSlim readerWriterLock;

        /// <summary>
        /// Dictionary of cache items for fast Get and Contains operations.
        /// </summary>
        private readonly Dictionary<long, MemoryCacheEntry> dictionary;

        /// <summary>
        /// Cache items sorted by the time of adding to cache. Required for to clear fast cache items when items are expired.
        /// </summary>
        private readonly SortedList<MemoryCacheEntry> sortedList;

        /// <summary>
        /// The maximum number of elements in the cache to avoid out of memory crashes.
        /// </summary>
        private readonly int maxSize;

        /// <summary>
        /// Timer for clearing expired cache items on recurring bases.
        /// </summary>
        private readonly Timer timer;

        /// <summary>
        /// The duration in milliseconds after which item in the cache is expired.
        /// </summary>
        private readonly int expirationMilliseconds;

        /// <summary>
        ///  Initializes a new instance of the <see cref="CacheProvider{TValue}" /> class.
        /// </summary>
        /// <param name="expirationMilliseconds">Expiration timeout in milliseconds for an object to live in the cache.</param>
        /// <param name="maxSize">Maximum number of entries to cache (adjustable at runtime with MaxSize property).</param>
        /// <param name="synchronized">True to use a reader-writer lock to protect the data in the MemoryCacheList; false if the caller will handle synchronization.</param>
        public CacheProvider(int expirationMilliseconds, int maxSize = 1000000, bool synchronized = true)
        {
            if (maxSize <= 0)
            {
                // The cache size must be a positive integer.
                throw new ArgumentOutOfRangeException("maxSize");
            }

            this.maxSize = maxSize;
            this.dictionary = new Dictionary<long, MemoryCacheEntry>();
            this.sortedList = new SortedList<MemoryCacheEntry>(new MemmoryCacheEntryTimeComparer());
            this.expirationMilliseconds = expirationMilliseconds;

            if (synchronized)
            {
                this.readerWriterLock = new ReaderWriterLockSlim();
            }

            this.timer = new Timer(this.ClearExpiredCacheItems, null, expirationMilliseconds, Timeout.Infinite);
        }

        /// <summary>
        /// Checks whether the cache entry already exists in the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>true if the cache contains a cache entry with the same key value as key; otherwise, false.</returns>
        public bool Contains(long key)
        {
            if (null != this.readerWriterLock)
            {
                this.readerWriterLock.EnterReadLock();
            }

            try
            {
                return this.dictionary.ContainsKey(key);
            }
            finally
            {
                if (null != this.readerWriterLock)
                {
                    this.readerWriterLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the specified cache entry from the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>A reference to the cache entry identified by key if the entry exists; otherwise, null.</returns>
        public TValue Get(long key)
        {
            if (null != this.readerWriterLock)
            {
                this.readerWriterLock.EnterReadLock();
            }

            try
            {
                TValue result;
                MemoryCacheEntry entry;

                if (!this.dictionary.TryGetValue(key, out entry))
                {
                    result = default(TValue);
                }
                else
                {
                    // It's safe to set this while only holding a reader lock.
                    result = entry.Value;
                }

                return result;
            }
            finally
            {
                if (null != this.readerWriterLock)
                {
                    this.readerWriterLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Inserts a cache entry into the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <param name="value">The object to insert.</param>
        public void Set(long key, TValue value)
        {
            this.Add(key, value, true);
        }

        /// <summary>
        /// Removes a specific key from the cache.
        /// </summary>
        /// <param name="key">Key to remove.</param>
        /// <returns>An object that represents the value of the removed cache entry that was specified by the key, or null if the specified entry was not found.</returns>
        public bool Remove(long key)
        {
            if (null != this.readerWriterLock)
            {
                this.readerWriterLock.EnterWriteLock();
            }

            try
            {
                MemoryCacheEntry entry;
                if (!this.dictionary.TryGetValue(key, out entry))
                {
                    return false;
                }

                this.dictionary.Remove(entry.Key);
                this.sortedList.Remove(entry);
                return true;
            }
            finally
            {
                if (null != this.readerWriterLock)
                {
                    this.readerWriterLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.timer != null)
            {
                this.timer.Dispose();
            }
        }

        /// <summary>
        /// The timer callback that clears expired items in the cache.
        /// </summary>
        /// <param name="state">An object containing information to be used by the callback method, or null.</param>
        private void ClearExpiredCacheItems(object state)
        {
            if (null != this.readerWriterLock)
            {
                this.readerWriterLock.EnterWriteLock();
            }

            try
            {
                var currentTicks = OperationWatch.ElapsedTicks;
                for (int i = 0; i < this.sortedList.Count; i++)
                {
                    var duration =
                        OperationWatch.Duration(this.sortedList[i].CreatedTicks, currentTicks).TotalMilliseconds;
                    if (duration > this.expirationMilliseconds)
                    {
                        var key = this.sortedList[i].Key;
                        this.dictionary.Remove(key);
                        this.sortedList.RemoveAt(i);
                    }
                    else
                    {
                        // list is sorted by elapsed ticks, we are not expected any expired anytimes anymore than
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DependencyCollectorEventSource.Log.RemoteDependencyModuleWarning(
                    string.Format(CultureInfo.InvariantCulture, "CacheProvider.ClearExpiredCacheItems failed. {0}", ex.ToInvariantString()));
            }
            finally
            {
                try
                {
                    // setup timer to next processing interval
                    this.timer.Change(this.expirationMilliseconds, Timeout.Infinite);
                }
                catch (Exception exc)
                {
                    DependencyCollectorEventSource.Log.RemoteDependencyModuleWarning(
                        string.Format(CultureInfo.InvariantCulture, "CacheProvider.ClearExpiredCacheItems failed. {0}", exc.ToInvariantString()));
                }
                finally
                {
                    if (null != this.readerWriterLock)
                    {
                        this.readerWriterLock.ExitWriteLock();
                    }
                }
            }
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">Key to add.</param>
        /// <param name="value">Value to associate with key.</param>
        /// <param name="overwrite">If true, will overwrite an existing key.</param>
        private void Add(long key, TValue value, bool overwrite)
        {
            if (null != this.readerWriterLock)
            {
                this.readerWriterLock.EnterWriteLock();
            }

            try
            {
                MemoryCacheEntry extantEntry;

                if (this.dictionary.TryGetValue(key, out extantEntry))
                {
                    if (!overwrite)
                    {
                        return;
                    }

                    // We already have an entry for this key. Update the entry
                    // with the new value and exit.
                    extantEntry.Value = value;
                }
                else if (this.dictionary.Count < this.maxSize)
                {
                    // The cache is still growing -- we do not need an eviction to add this entry.
                    MemoryCacheEntry newEntry = new MemoryCacheEntry(key, value);
                    this.dictionary.Add(key, newEntry);
                    this.sortedList.Add(newEntry);
                }
            }
            finally
            {
                if (null != this.readerWriterLock)
                {
                    this.readerWriterLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// An entry in the MemoryCacheList.
        /// </summary>
        private class MemoryCacheEntry
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MemoryCacheEntry" /> class.
            /// </summary>
            /// <param name="key">The key of the element.</param>
            /// <param name="value">The value of the element.</param>
            internal MemoryCacheEntry(long key, TValue value)
            {
                this.Key = key;
                this.Value = value;
                this.CreatedTicks = OperationWatch.ElapsedTicks;
            }

            /// <summary>
            /// Gets the key of the element.
            /// </summary>
            internal long Key { get; private set; }

            /// <summary>
            /// Gets or sets the value of the element.
            /// </summary>
            internal TValue Value { get; set; }

            /// <summary>
            /// Gets number of ticks elapsed on the clock since the element was created.
            /// </summary>
            internal long CreatedTicks { get; private set; }
        }

        /// <summary>
        /// Exposes a method that compares two MemoryCacheEntry objects.
        /// </summary>
        private class MemmoryCacheEntryTimeComparer : IComparer<MemoryCacheEntry>
        {
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>A signed integer that indicates the relative values of x and y, as shown in the following table.</returns>
            public int Compare(MemoryCacheEntry x, MemoryCacheEntry y)
            {
                int result;
                if (x == null && y == null)
                {
                    result = 0;
                }
                else if (x == null)
                {
                    result = -1;
                }
                else if (y == null)
                {
                    result = 1;
                }
                else
                {
                    // x != null and y != null
                    if (x.CreatedTicks < y.CreatedTicks)
                    {
                        result = -1;
                    }
                    else if (x.CreatedTicks > y.CreatedTicks)
                    {
                        result = 1;
                    }
                    else
                    {
                        result = 0;
                    }
                }

                return result;
            }
        }
    }

    // #else
    /*
    internal class CacheProvider<TValue> : ICacheProvider<TValue>
    {
        private readonly System.Runtime.Caching.ObjectCache requestCache = System.Runtime.Caching.MemoryCache.Default;

        /// <summary>
        /// The cache item policy which identifies the expiration time.
        /// </summary>
        private readonly System.Runtime.Caching.CacheItemPolicy cacheItemPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheProvider{TValue}" /> class.
        /// </summary>
        /// <param name="expirationMilliseconds">Time in milliseconds for an item to expire in the cache.</param>
        public CacheProvider(int expirationMilliseconds)
        {
            // Setting expiration timeout to 100 seconds as it is the default timeout on HttpWebRequest object
            this.cacheItemPolicy = new System.Runtime.Caching.CacheItemPolicy { SlidingExpiration = TimeSpan.FromMilliseconds(100) };
        }

        /// <summary>
        /// Checks whether the cache entry already exists in the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>true if the cache contains a cache entry with the same key value as key; otherwise, false.</returns>
        public bool Contains(string key)
        {
            return this.requestCache.Contains(key);
        }

        /// <summary>
        /// Gets the specified cache entry from the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>A reference to the cache entry identified by key if the entry exists; otherwise, null.</returns>
        public TValue Get(string key)
        {
            TValue result = default(TValue);
            var cacheItem = this.requestCache.GetCacheItem(key);
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
            this.requestCache.Set(key, value, this.cacheItemPolicy);
        }

        /// <summary>
        /// Removes the cache entry from the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>True if the element is successfully found and removed; otherwise, false. This method returns false if key is not found.</returns>
        public bool Remove(string key)
        {
            return this.requestCache.Remove(key) != null;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // there is nothing to dispose here.
        }
    }
     
     */

    // #endif
}
