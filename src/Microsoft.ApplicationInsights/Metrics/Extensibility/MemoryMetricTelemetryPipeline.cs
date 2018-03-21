namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    public class MemoryMetricTelemetryPipeline : IMetricTelemetryPipeline, IReadOnlyList<MetricAggregate>
    {
        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public const int CountLimitDefault = 1000;

        private readonly Task _completedTask = Task.FromResult(true);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        private readonly IList<MetricAggregate> _metricAgregates = new List<MetricAggregate>();

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public MemoryMetricTelemetryPipeline()
            : this(CountLimitDefault)
        {
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="countLimit">ToDo: Complete documentation before stable release.</param>
        public MemoryMetricTelemetryPipeline(int countLimit)
        {
            if (countLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(countLimit));
            }

            this.CountLimit = countLimit;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int CountLimit { get; }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int Count
        {
            get
            {
                int count;
                this._lock.WaitAsync().GetAwaiter().GetResult();
                try
                {
                    count = this._metricAgregates.Count;
                }
                finally
                {
                    this._lock.Release();
                }

                return count;
            }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="index">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public MetricAggregate this[int index]
        {
            get
            {
                MetricAggregate metricAggregate;
                this._lock.WaitAsync().GetAwaiter().GetResult();
                try
                {
                    metricAggregate = this._metricAgregates[index];
                }
                finally
                {
                    this._lock.Release();
                }

                return metricAggregate;
            }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public void Clear()
        {
            this._lock.WaitAsync().GetAwaiter().GetResult();
            try
            {
                this._metricAgregates.Clear();
            }
            finally
            {
                this._lock.Release();
            }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricAggregate">ToDo: Complete documentation before stable release.</param>
        /// <param name="cancelToken">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public async Task TrackAsync(MetricAggregate metricAggregate, CancellationToken cancelToken)
        {
            Util.ValidateNotNull(metricAggregate, nameof(metricAggregate));

            await this._lock.WaitAsync(cancelToken);
            try
            {
                while (this._metricAgregates.Count >= this.CountLimit)
                {
                    this._metricAgregates.RemoveAt(0);
                }

                this._metricAgregates.Add(metricAggregate);
            }
            finally
            {
                this._lock.Release();
            }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="cancelToken">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public Task FlushAsync(CancellationToken cancelToken)
        {
            return Task.FromResult(true);
        }

        IEnumerator<MetricAggregate> IEnumerable<MetricAggregate>.GetEnumerator()
        {
            IEnumerator<MetricAggregate> enumerator;
            this._lock.WaitAsync().GetAwaiter().GetResult();
            try
            {
                enumerator = this._metricAgregates.GetEnumerator();
            }
            finally
            {
                this._lock.Release();
            }

            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<MetricAggregate>)this).GetEnumerator();
        }
    }
}
