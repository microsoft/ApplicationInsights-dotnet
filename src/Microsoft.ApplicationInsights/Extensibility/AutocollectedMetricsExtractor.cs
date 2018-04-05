namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Extracts auto-collected, pre-aggregated (aka. "standard") metrics from telemetry.
    /// Metric Extractors participate in the telemetry pipeline as telemetry processors. They examine telemetry items going through
    /// the pipeline and create pre-aggregated metrics based on the encountered items. The metrics can be anything. For example, one may
    /// choose to extract a metric for "Request Duration" from RequestTelemetry items. Or one may choose to create a metric "Cows Sold"
    /// from specific user-tracked EventTelemetry items that contain respective information. 
    /// <br />
    /// Metric Extractors should be placed into the pipeline after telemetry initializers and before any telemetry processors that may
    /// perform any kind of filtering, e.g. before any sampling processors. Placing metric extractors after any filters will prevent them
    /// from seeing all potentially relevant telemetry which will skew the extracted metrics.
    /// <br />
    /// This extractor is responsible for aggregating auto-collected, pre-aggregated (aka. "standard") metrics, such as failed request
    /// count, dependency call durations and similar. Users may use the same pattern to create their own extractors for any metrics
    /// they want from any kind of telemetry. 
    /// This extractor contains several implementations of the (internal) <c>ISpecificAutocollectedMetricsExtractor</c>-interface to which
    /// it delegates the aggregation of particular metrics. All those implementations share the
    /// same <see cref="Microsoft.ApplicationInsights.Extensibility.MetricManagerV1"/>-instance for metric aggregation.
    /// </summary>
    public sealed class AutocollectedMetricsExtractor : ITelemetryProcessor, ITelemetryModule, IDisposable
    {
        // List of all participating extractors that take care of specific metrics kinds:
        private readonly RequestMetricsExtractor extractorForRequestMetrics;
        private readonly DependencyMetricsExtractor extractorForDependencyMetrics;

        /// <summary>
        /// We have dedicated instance variables to refer to each individual extractors because we are exposing some of their properties to the config subsystem here.
        /// However, for calling common methods for all of them, we also group them together.
        /// </summary>
        private readonly IEnumerable<ExtractorWithInfo> extractors;

        /// <summary>
        /// Gets the metric manager that owns all extracted metric data series.
        /// The <c>MetricManager</c> allows participating extractors to access the <c>Microsoft.ApplicationInsights.Extensibility.MetricManager</c> instance
        /// that aggregates all metrics to be extracted. Participants should call
        /// <see cref="MetricManagerV1.CreateMetric(string, System.Collections.Generic.IDictionary{string, string})"/> on
        /// this instance for construction of all data series to be extracted from telemetry. This will ensure that all metric documents are
        /// aggregated and tagged correctly and are processed using the <see cref="TelemetryConfiguration"/> instance used to initialize this extractor.
        /// </summary>
        private MetricManagerV1 metricManager = null;

        /// <summary>
        /// The telemetry processor that will be called after this processor.
        /// </summary>
        private ITelemetryProcessor nextProcessorInPipeline = null;

        /// <summary>
        /// Marks if we ever log MetricExtractorAfterSamplingError so that if we do we use Verbosity level subsequently.
        /// </summary>
        private bool isMetricExtractorAfterSamplingLogged = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutocollectedMetricsExtractor" /> class.
        /// </summary>
        /// <param name="nextProcessorInPipeline">Subsequent telemetry processor.</param>
        public AutocollectedMetricsExtractor(ITelemetryProcessor nextProcessorInPipeline)
        {
            this.nextProcessorInPipeline = nextProcessorInPipeline;

            this.extractorForRequestMetrics = new RequestMetricsExtractor();
            this.extractorForDependencyMetrics = new DependencyMetricsExtractor();

            this.extractors = new ExtractorWithInfo[]
                    {
                        new ExtractorWithInfo(this.extractorForRequestMetrics, GetExtractorInfo(this.extractorForRequestMetrics)),
                        new ExtractorWithInfo(this.extractorForDependencyMetrics, GetExtractorInfo(this.extractorForDependencyMetrics)),
                    };
        }

        /// <summary>
        /// Gets or sets the <see cref="DependencyMetricsExtractor.MaxDependencyTypesToDiscover"/>-property.
        /// See the remarks for the <see cref="DependencyMetricsExtractor"/>-class for more info.
        /// </summary>
        public int MaxDependencyTypesToDiscover
        {
            get
            {
                return this.extractorForDependencyMetrics.MaxDependencyTypesToDiscover;
            }

            set
            {
                this.extractorForDependencyMetrics.MaxDependencyTypesToDiscover = value;
            }
        }

        /// <summary>
        /// This class implements the <see cref="ITelemetryModule"/> interface by defining this method.
        /// It will be called by the infrastructure when the telemetry pipeline is being built.
        /// This will ensure that the extractor is initialized using the same <see cref="TelemetryConfiguration"/> as the rest of the pipeline.
        /// Specifically, this will also ensure that the <see cref="Microsoft.ApplicationInsights.Extensibility.MetricManagerV1"/> and its
        /// respective <see cref="TelemetryClient"/> used internally for sending extracted metrics use the same configuration.
        /// </summary>
        /// <param name="configuration">The telemetric configuration to be used by this extractor.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            TelemetryClient telemetryClient = (configuration == null)
                                                    ? new TelemetryClient()
                                                    : new TelemetryClient(configuration);

            if (!string.IsNullOrWhiteSpace(MetricTerms.Autocollection.Moniker.Key))
            {
                telemetryClient.Context.Properties[MetricTerms.Autocollection.Moniker.Key] = MetricTerms.Autocollection.Moniker.Value;
            }

            this.metricManager = new MetricManagerV1(telemetryClient);
            this.InitializeExtractors();
        }

        /// <summary>
        /// This class implements the <see cref="ITelemetryProcessor"/> interface by defining this method.
        /// This method will be called by the pipeline for each telemetry item that goes through it.
        /// It invokes <see cref="ExtractMetrics(ITelemetry)"/> to actually do the extraction.
        /// </summary>
        /// <param name="item">The telemetry item from which the metrics will be extracted.</param>
        public void Process(ITelemetry item)
        {
            if (item != null)
            {
                this.ExtractMetrics(item);
            }

            this.InvokeNextProcessor(item);
        }

        /// <summary>
        /// Disposes this telemetry extractor.
        /// </summary>
        public void Dispose()
        {
            IDisposable metricMgr = this.metricManager;
            if (metricMgr != null)
            {
                // benign race
                metricMgr.Dispose();
                this.metricManager = null;
            }
        }

        /// <summary>
        /// Constructs the extractor info string for caching.
        /// </summary>
        /// <param name="extractor">The extractor to describe.</param>
        /// <returns>Extractor info string for caching.</returns>
        private static string GetExtractorInfo(ISpecificAutocollectedMetricsExtractor extractor)
        {
            string extractorName;
            string extractorVersion;

            try
            {
                extractorName = extractor?.ExtractorName ?? "null";
            }
            catch
            {
                extractorName = extractor.GetType().FullName;
            }

            try
            {
                extractorVersion = extractor.ExtractorVersion ?? "null";
            }
            catch
            {
                extractorVersion = "unknown";
            }

            string extractorInfo = string.Format(
                                        CultureInfo.InvariantCulture,
                                        MetricTerms.Extraction.ProcessedByExtractors.Moniker.ExtractorInfoTemplate,
                                        extractorName,
                                        extractorVersion);
            return extractorInfo;
        }

        /// <summary>
        /// All telemetry that has been processed by this extractor will be tagged by adding the
        /// string "<c>(Name: {ExtractorName}, Ver:{ExtractorVersion})</c>" to the <c>xxx.ProcessedByExtractors</c> property.
        /// This method adds that string to the specified telemetry item's properties.
        /// </summary>
        /// <param name="item">The telemetry item to be tagged.</param>
        /// <param name="extractorInfo">The string to be added to the item's properties.</param>
        private static void AddExtractorInfo(ITelemetry item, string extractorInfo)
        {
            string extractionPipelineInfo;
            bool hasPrevInfo = item.Context.Properties.TryGetValue(MetricTerms.Extraction.ProcessedByExtractors.Moniker.Key, out extractionPipelineInfo);

            if (!hasPrevInfo)
            {
                extractionPipelineInfo = string.Empty;
            }
            else
            {
                if (extractionPipelineInfo.Length > 0)
                {
                    extractionPipelineInfo = extractionPipelineInfo + "; ";
                }
            }

            extractionPipelineInfo = extractionPipelineInfo + extractorInfo;
            item.Context.Properties[MetricTerms.Extraction.ProcessedByExtractors.Moniker.Key] = extractionPipelineInfo;
        }

        /// <summary>
        /// Calls all participating extractors to initialize themselves.
        /// </summary>
        private void InitializeExtractors()
        {
            MetricManagerV1 thisMetricManager = this.metricManager;

            foreach (ExtractorWithInfo participant in this.extractors)
            {
                try
                {
                    participant.Extractor.InitializeExtractor(thisMetricManager);
                }
                catch (Exception ex)
                {
                    CoreEventSource.Log.LogError("Error in " + participant.Info + ": " + ex.ToString());
                }
            }
        }

        /// <summary>
        /// Calls the <see cref="ISpecificAutocollectedMetricsExtractor.ExtractMetrics(ITelemetry, out bool)"/> of each participating extractor for the specified item.
        /// Catches and logs all errors.
        /// If <c>isItemProcessed</c> is True, adds a corresponding marker to the item's properties.
        /// </summary>
        /// <param name="fromItem">The item from which to extract metrics.</param>
        private void ExtractMetrics(ITelemetry fromItem)
        {
            //// Workaround: There is a suspected but unconfirmed issue around Extractor performance with telemetry from which no metrics need to be extracted.
            //// Putting this IF as a temporary workaround until this can be investigated. 
            if (!((fromItem is RequestTelemetry) || (fromItem is DependencyTelemetry)))
            {
                return;
            }

            if (!this.EnsureItemNotSampled(fromItem))
            {
                return;
            }

            foreach (ExtractorWithInfo participant in this.extractors)
            {
                try
                {
                    bool isItemProcessed;
                    participant.Extractor.ExtractMetrics(fromItem, out isItemProcessed);

                    if (isItemProcessed)
                    {
                        AddExtractorInfo(fromItem, participant.Info);
                    }
                }
                catch (Exception ex)
                {
                    CoreEventSource.Log.LogError("Error in " + participant.Extractor.GetType().Name + ": " + ex.ToString());
                }
            }
        }

        private bool EnsureItemNotSampled(ITelemetry item)
        {
            ISupportSampling potentiallySampledItem = item as ISupportSampling;

            if (potentiallySampledItem != null
                    && potentiallySampledItem.SamplingPercentage.HasValue
                    && potentiallySampledItem.SamplingPercentage.Value < (100.0 - 1.0E-12))
            {
                if (!this.isMetricExtractorAfterSamplingLogged)  
                {
                    //// benign race
                    this.isMetricExtractorAfterSamplingLogged = true;
                    CoreEventSource.Log.MetricExtractorAfterSamplingError();
                }
                else
                {
                    CoreEventSource.Log.MetricExtractorAfterSamplingVerbose();
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Invokes the subsequent telemetry processor if it has been initialized.
        /// </summary>
        /// <param name="item">Item to pass.</param>
        private void InvokeNextProcessor(ITelemetry item)
        {
            ITelemetryProcessor next = this.nextProcessorInPipeline;
            if (next != null)
            {
                next.Process(item);
            }
        }

        /// <summary>
        /// Groups an instance of <c>ISpecificAutocollectedMetricsExtractor</c> with a cached version of it's pipeline processing info.
        /// </summary>
        private class ExtractorWithInfo
        {
            public ExtractorWithInfo(ISpecificAutocollectedMetricsExtractor extractor, string info)
            {
                this.Extractor = extractor;
                this.Info = info;
            }

            public ISpecificAutocollectedMetricsExtractor Extractor { get; private set; }

            public string Info { get; private set; }
        }
    }
}
