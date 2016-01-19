namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation
{
    /// <summary>
    /// Represents an object cache and provides the base methods and properties for accessing the object cache.
    /// </summary>
    internal interface ICacheProvider<TValue> : System.IDisposable
    {
        /// <summary>
        /// Checks whether the cache entry already exists in the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>true if the cache contains a cache entry with the same key value as key; otherwise, false.</returns>
        bool Contains(long key);

        /// <summary>
        /// Gets the specified cache entry from the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>A reference to the cache entry identified by key if the entry exists; otherwise, null.</returns>
        TValue Get(long key);

        /// <summary>
        /// Inserts a cache entry into the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <param name="value">The object to insert.</param>
        void Set(long key, TValue value);

        /// <summary>
        /// Removes the cache entry from the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <returns>True if the element is successfully found and removed; otherwise, false. This method returns false if key is not found.</returns>
        bool Remove(long key);
    }
}
