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

            CountLimit = countLimit;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int CountLimit { get; }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int Count
        {
            get
            {
                int count;
                _lock.WaitAsync().GetAwaiter().GetResult();
                try
                {
                    count = _metricAgregates.Count;
                }
                finally
                {
                    _lock.Release();
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
                _lock.WaitAsync().GetAwaiter().GetResult();
                try
                {
                    metricAggregate = _metricAgregates[index];
                }
                finally
                {
                    _lock.Release();
                }

                return metricAggregate;
            }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public void Clear()
        {
            _lock.WaitAsync().GetAwaiter().GetResult();
            try
            {
                _metricAgregates.Clear();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricAggregate">ToDo: Complete documentation before stable release.</param>
        /// <param name="cancelToken">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public async Task TrackAsync(MetricAggregate metricAggregate, CancellationToken cancelToken)
        {
            Util.ValidateNotNull(metricAggregate, nameof(metricAggregate));

            await _lock.WaitAsync(cancelToken);
            try
            {
                while (_metricAgregates.Count >= CountLimit)
                {
                    _metricAgregates.RemoveAt(0);
                }

                _metricAgregates.Add(metricAggregate);
            }
            finally
            {
                _lock.Release();
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
            _lock.WaitAsync().GetAwaiter().GetResult();
            try
            {
                enumerator = _metricAgregates.GetEnumerator();
            }
            finally
            {
                _lock.Release();
            }

            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<MetricAggregate>)this).GetEnumerator();
        }
    }
}
