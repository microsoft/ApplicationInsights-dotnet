// <copyright file="FixedSizeQueue.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A light fixed size queue. If Enqueue is called and queue's limit has reached the last item will be removed.
    /// This data structure is thread safe.
    /// </summary>
    internal class FixedSizeQueue<T>
    {
        private readonly int maxSize;
        private object queueLockObj = new object();
        private Queue<T> queue = new Queue<T>();

        internal FixedSizeQueue(int maxSize)
        {
            this.maxSize = maxSize;
        }

        internal void Enqueue(T item)
        {
            lock (this.queueLockObj)
            {
                if (this.queue.Count == this.maxSize)
                {
                    this.queue.Dequeue();
                }

                this.queue.Enqueue(item);
            }
        }

        internal bool Contains(T item)
        {
            lock (this.queueLockObj)
            {
                return this.queue.Contains(item);
            }
        }
    }
}
