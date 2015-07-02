// <copyright file="SnapshottingDictionary.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;

    internal class SnapshottingDictionary<TKey, TValue> : SnapshottingCollection<KeyValuePair<TKey, TValue>, IDictionary<TKey, TValue>>, IDictionary<TKey, TValue>
    {
        public SnapshottingDictionary()
            : base(new Dictionary<TKey, TValue>())
        {
        }

        public ICollection<TKey> Keys
        {
            get { return this.GetSnapshot().Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return this.GetSnapshot().Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                return this.GetSnapshot()[key];
            }

            set
            {
                lock (this.Collection)
                {
                    this.Collection[key] = value;
                    this.snapshot = null;
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (this.Collection)
            {
                this.Collection.Add(key, value);
                this.snapshot = null;
            }
        }

        public bool ContainsKey(TKey key)
        {
            return this.GetSnapshot().ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            lock (this.Collection)
            {
                bool removed = this.Collection.Remove(key);
                if (removed)
                {
                    this.snapshot = null;
                }

                return removed;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.GetSnapshot().TryGetValue(key, out value);
        }

        protected sealed override IDictionary<TKey, TValue> CreateSnapshot(IDictionary<TKey, TValue> collection)
        {
            return new Dictionary<TKey, TValue>(collection);
        }
    }
}
