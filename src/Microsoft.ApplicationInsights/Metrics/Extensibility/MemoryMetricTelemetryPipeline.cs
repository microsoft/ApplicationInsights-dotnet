namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>@ToDo: Complete documentation before stable release. {079}</summary>
    /// @PublicExposureCandidate
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001: Types that own disposable fields should be disposable", Justification = "OK not to explicitly dispose a released SemaphoreSlim.")]
    internal class MemoryMetricTelemetryPipeline : IMetricTelemetryPipeline, IReadOnlyList<MetricAggregate>
    {
        /// <summary>@ToDo: Complete documentation before stable release. {529}</summary>
        public const int CountLimitDefault = 1000;

        private readonly Task completedTask = Task.FromResult(true);
        private readonly SemaphoreSlim updateLock = new SemaphoreSlim(1);

        private readonly IList<MetricAggregate> metricAgregates = new List<MetricAggregate>();

        /// <summary>@ToDo: Complete documentation before stable release. {846}</summary>
        public MemoryMetricTelemetryPipeline()
            : this(CountLimitDefault)
        {
        }

        /// <summary>@ToDo: Complete documentation before stable release. {195}</summary>
        /// <param name="countLimit">@ToDo: Complete documentation before stable release. {153}</param>
        public MemoryMetricTelemetryPipeline(int countLimit)
        {
            if (countLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(countLimit));
            }

            this.CountLimit = countLimit;
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {953}</summary>
        public int CountLimit { get; }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {917}</summary>
        public int Count
        {
            get
            {
                int count;
                this.updateLock.WaitAsync().GetAwaiter().GetResult();
                try
                {
                    count = this.metricAgregates.Count;
                }
                finally
                {
                    this.updateLock.Release();
                }

                return count;
            }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {823}</summary>
        /// <param name="index">@ToDo: Complete documentation before stable release. {470}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {699}</returns>
        public MetricAggregate this[int index]
        {
            get
            {
                MetricAggregate metricAggregate;
                this.updateLock.WaitAsync().GetAwaiter().GetResult();
                try
                {
                    metricAggregate = this.metricAgregates[index];
                }
                finally
                {
                    this.updateLock.Release();
                }

                return metricAggregate;
            }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {169}</summary>
        public void Clear()
        {
            this.updateLock.WaitAsync().GetAwaiter().GetResult();
            try
            {
                this.metricAgregates.Clear();
            }
            finally
            {
                this.updateLock.Release();
            }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {319}</summary>
        /// <param name="metricAggregate">@ToDo: Complete documentation before stable release. {915}</param>
        /// <param name="cancelToken">@ToDo: Complete documentation before stable release. {929}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {190}</returns>
        public async Task TrackAsync(MetricAggregate metricAggregate, CancellationToken cancelToken)
        {
            Util.ValidateNotNull(metricAggregate, nameof(metricAggregate));

            await this.updateLock.WaitAsync(cancelToken);
            try
            {
                while (this.metricAgregates.Count >= this.CountLimit)
                {
                    this.metricAgregates.RemoveAt(0);
                }

                this.metricAgregates.Add(metricAggregate);
            }
            finally
            {
                this.updateLock.Release();
            }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {094}</summary>
        /// <param name="cancelToken">@ToDo: Complete documentation before stable release. {776}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {084}</returns>
        public Task FlushAsync(CancellationToken cancelToken)
        {
            return Task.FromResult(true);
        }

        IEnumerator<MetricAggregate> IEnumerable<MetricAggregate>.GetEnumerator()
        {
            IEnumerator<MetricAggregate> enumerator;
            this.updateLock.WaitAsync().GetAwaiter().GetResult();
            try
            {
                enumerator = this.metricAgregates.GetEnumerator();
            }
            finally
            {
                this.updateLock.Release();
            }

            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<MetricAggregate>)this).GetEnumerator();
        }
    }
}
