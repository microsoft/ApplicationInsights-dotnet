using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// </summary>
    public class MemoryMetricTelemetryPipeline : IMetricTelemetryPipeline, IReadOnlyList<object>
    {
        public const int CountLimitDefault = 1000;

        private readonly Task _completedTask = Task.FromResult(true);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        private readonly IList<object> _metricAgregates = new List<object>();

        /// <summary>
        /// </summary>
        public MemoryMetricTelemetryPipeline()
            : this(CountLimitDefault)
        {
        }

        public MemoryMetricTelemetryPipeline(int countLimit)
        {
            if (countLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(countLimit));
            }

            CountLimit = countLimit;
        }

        public int CountLimit { get; }

        public int Count {
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

        public object this[int index]
        {
            get
            {
                object metricAggregate;
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

        /// <summary>
        /// </summary>
        /// <param name="metricAggregate"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task TrackAsync(object metricAggregate, CancellationToken cancelToken)
        {
            Util.ValidateNotNull(metricAggregate, nameof(metricAggregate));

            await _lock.WaitAsync(cancelToken);
            try
            {
                while( _metricAgregates.Count >= CountLimit)
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

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            IEnumerator<object> enumerator;
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
            return ((IEnumerable<object>) this).GetEnumerator();
        }
    }
}
