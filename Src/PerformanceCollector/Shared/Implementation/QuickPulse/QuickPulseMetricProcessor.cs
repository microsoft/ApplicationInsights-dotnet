namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Filtering;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;

    /// <summary>
    /// Metric processor for collecting QuickPulse data.
    /// </summary>
    internal sealed class QuickPulseMetricProcessor : IMetricProcessor
    {
        private bool isCollecting = false;

        private IQuickPulseDataAccumulatorManager dataAccumulatorManager;

        public void StartCollection(IQuickPulseDataAccumulatorManager accumulatorManager)
        {
            if (accumulatorManager == null)
            {
                throw new ArgumentNullException(nameof(accumulatorManager));
            }

            if (this.isCollecting)
            {
                throw new InvalidOperationException("Can't start collection while it is already running.");
            }

            this.dataAccumulatorManager = accumulatorManager;
            
            this.isCollecting = true;
        }

        public void StopCollection()
        {
            this.isCollecting = false;
            this.dataAccumulatorManager = null;
        }
        
        public void Track(Metric metric, double value)
        {
            try
            {
                if (!this.isCollecting || this.dataAccumulatorManager == null || metric == null)
                {
                    return;
                }

                // get a local reference, the accumulator might get swapped out a any time
                // in case we continue to process this configuration once the accumulator is out, increase the reference count so that this accumulator is not sent out before we're done
                CollectionConfigurationAccumulator configurationAccumulatorLocal =
                    this.dataAccumulatorManager.CurrentDataAccumulator.CollectionConfigurationAccumulator;

                // if the accumulator is swapped out, a sample is created and sent out - all while between these two lines, this telemetry item gets lost
                // however, that is not likely to happen
                configurationAccumulatorLocal.AddRef();
                try
                {
                    foreach (Tuple<string, string, AggregationType> metricToCollect in configurationAccumulatorLocal.CollectionConfiguration.MetricMetrics)
                    {
                        if (string.Equals(metric.Name, metricToCollect.Item2, StringComparison.OrdinalIgnoreCase))
                        {
                            configurationAccumulatorLocal.MetricAccumulators[metricToCollect.Item1].AddValue(value);
                            break;
                        }
                    }
                }
                finally
                {
                    configurationAccumulatorLocal.Release();
                }
            }
            catch (Exception e)
            {
                // whatever happened up there - we don't want to interrupt the flow of telemetry
                QuickPulseEventSource.Log.UnknownErrorEvent(e.ToInvariantString());
            }
        }
    }
}