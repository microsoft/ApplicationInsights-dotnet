namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Globalization;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Abstract base class for Metric Extractors.
    /// Metric Extractors participate in the telemetry pipeline as telemetry processors. They examine telemetry items going through
    /// the pipeline and create pre-aggregated metrics based on the encountered items. The metrics can be anything. For example, one may
    /// choose to extract a metric for "Request Duration" from RequestTelemetry items. Or one may choose to create a metric "Cows Sold"
    /// from specific user-tracked EventTelemetry items that contain respective information. 
    /// Metric Extractors should be placed into the pipeline after telemetry initializers and before any telemetry processors that may
    /// perform any kind of filtering, e.g. before any sampling processors. Placing metric extractors after any filters will prevent them
    /// from seeing all potentially relevant telemetry which will skew the extracted metrics.
    /// </summary>
    public abstract class MetricExtractorTelemetryProcessorBase : ITelemetryProcessor, ITelemetryModule, IDisposable
    {
        private MetricManager metricManager = null;
        private ITelemetryProcessor nextProcessorInPipeline = null;
        private string extractorInfo = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricExtractorTelemetryProcessorBase" /> class.
        /// </summary>
        /// <param name="nextProcessorInPipeline">Subsequent telemetry processor.</param>
        public MetricExtractorTelemetryProcessorBase(ITelemetryProcessor nextProcessorInPipeline)
            : this(nextProcessorInPipeline, metricTelemetryMarkerKey: null, metricTelemetryMarkerValue: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricExtractorTelemetryProcessorBase" /> class.
        /// </summary>
        /// <param name="nextProcessorInPipeline">Subsequent telemetry processor.</param>
        /// <param name="metricTelemetryMarkerKey">All emitted MetricTelemetric documents will be automatically marked with a property
        /// with this key and the value specified in <c>metricTelemetryMarkerValue</c>.
        /// Specify <c>null</c> to avoid tagging produced metric documents.</param>
        /// <param name="metricTelemetryMarkerValue">All emitted MetricTelemetric documents will be automatically marked with a property
        /// with the key specified in <c>metricTelemetryMarkerKey</c> and with this value.</param>
        public MetricExtractorTelemetryProcessorBase(
                                            ITelemetryProcessor nextProcessorInPipeline,
                                            string metricTelemetryMarkerKey,
                                            string metricTelemetryMarkerValue)
        {
            this.nextProcessorInPipeline = nextProcessorInPipeline;

            if (metricTelemetryMarkerKey != null)
            {
                metricTelemetryMarkerKey = metricTelemetryMarkerKey.Trim();
                if (metricTelemetryMarkerKey.Length == 0)
                {
                    metricTelemetryMarkerKey = null;
                }
            }

            if (metricTelemetryMarkerValue != null)
            {
                metricTelemetryMarkerValue = metricTelemetryMarkerValue.Trim();
            }
            else
            {
                metricTelemetryMarkerValue = string.Empty;
            }

            this.MetricTelemetryMarkerKey = metricTelemetryMarkerKey;
            this.MetricTelemetryMarkerValue = metricTelemetryMarkerValue;
        }

        /// <summary>
        /// Gets: All emitted MetricTelemetric documents will be automatically marked with a property
        /// with this key and the value specified in <c>MetricTelemetryMarkerValue</c>.
        /// Specify <c>null</c> to avoid tagging produced metric documents.
        /// </summary>
        public string MetricTelemetryMarkerKey { get; private set; }

        /// <summary>
        /// Gets: All emitted MetricTelemetric documents will be automatically marked with a property
        /// with the key specified in <c>MetricTelemetryMarkerValue</c> and with this value .
        /// </summary>
        public string MetricTelemetryMarkerValue { get; private set; }

        /// <summary>
        /// Gets: All telemetry that has been processed by this extractor will be tagged by adding the
        /// string "<c>(Name: {ExtractorName}, Ver:{ExtractorVersion})</c>" to the <c>xxx.ProcessedByExtractors</c> property.
        /// Implementations must override this getter to get their version. 
        /// The results of this calling this getter may be cached, so it must be idempotent and it must always return the same value.
        /// </summary>
        protected abstract string ExtractorVersion { get; }

        /// <summary>
        /// Gets: Allows derived classes to access the <c>Microsoft.ApplicationInsights.Extensibility.MetricManager</c> instance that aggregates
        /// all metrics to be extracted. Implementations should call
        /// <see cref="MetricManager.CreateMetric(string, System.Collections.Generic.IDictionary{string, string})"/> on
        /// this instance for construction of all data series to be extracted from telemetry. This will ensure that all metric documents are
        /// aggregated and tagged correctly and are processed using the <see cref="TelemetryConfiguration"/> instance used to initialize this extractor.
        /// </summary>
        protected MetricManager MetricManager
        {
            get { return this.metricManager; }
        }

        /// <summary>
        /// Gets: All telemetry that has been processed by this extractor will be tagged by adding the
        /// string "<c>(Name: {ExtractorName}, Ver:{ExtractorVersion})</c>" to the <c>xxx.ProcessedByExtractors</c> property.
        /// Implementations can override this getter to get their name. By default the <c>FullName</c> of the type will be used.
        /// The results of this calling this getter may be cached, so it must be idempotent and it must always return the same value.
        /// </summary>
        protected virtual string ExtractorName
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        /// <summary>
        /// This class implements the <see cref="ITelemetryModule"/> interface by defining this method.
        /// It will be called by the infrastructure when the telemetry pipeline is being built.
        /// This will ensure that the extractor is initialized using the same <see cref="TelemetryConfiguration"/> as the rest of the pipeline.
        /// Specifically, this will also ensure that the <see cref="Microsoft.ApplicationInsights.Extensibility.MetricManager"/> and its
        /// respective <see cref="TelemetryClient"/> used internally for sending extracted metrics use the same configuration.
        /// Do not override this method. In order to construct <see cref="Metric"/> objects for the extracted metrics (and for other initialization),
        /// override the <see cref="InitializeExtractor(TelemetryConfiguration)"/> instead. It will be invoked after the
        /// internal <see cref="MetricManager"/> has been constructed, such that it is available for use.
        /// </summary>
        /// <param name="configuration">The telemetric configuration to be used by this extractor.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            TelemetryClient telemetryClient = (configuration == null)
                                                    ? new TelemetryClient()
                                                    : new TelemetryClient(configuration);
            if (this.MetricTelemetryMarkerKey != null)
            {
                telemetryClient.Context.Properties[this.MetricTelemetryMarkerKey] = this.MetricTelemetryMarkerValue;
            }

            this.metricManager = new MetricManager(telemetryClient);

            this.InitializeExtractor(configuration);
        }

        /// <summary>
        /// This class implements the <see cref="ITelemetryProcessor"/> interface by defining this method.
        /// This method will be called by the pipeline for each telemetry item that goes through it.
        /// The method calls <see cref="ExtractMetrics(ITelemetry, out bool)"/> that needs to be overridden to actually extract the metrics.
        /// Do not override this method. It ensures that potential errors are handled and logged correctly, that all telemetry is tagged as expected,
        /// and that subsequent telemetry processors are invoked.
        /// </summary>
        /// <param name="item">The telemetry item from which the metrics will be extracted.</param>
        public void Process(ITelemetry item)
        {
            if (item == null)
            {
                this.InvokeNextProcessor(item);
                return;
            }

            try
            {
                bool isItemProcessed;
                this.ExtractMetrics(item, out isItemProcessed);

                if (isItemProcessed)
                {
                    this.AddExtractorInfo(item);
                }
            }
            catch (Exception ex)
            {
                CoreEventSource.Log.LogError("Error in Metric Extractor named: " + ex.ToString());
            }

            this.InvokeNextProcessor(item);
        }

        /// <summary>
        /// Implementations must override this method in order to to any initialization required.
        /// For example, this is a chance to pre-construct any <see cref="Metric"/> instances that will be used to extract
        /// and aggregate metric data series. The <see cref="MetricManager"/> property that should be used to construct
        /// all <c>Metric</c>s emitted by this extractor will be readily initialized when this method is invoked.
        /// </summary>
        /// <param name="configuration">Config to use.</param>
        public abstract void InitializeExtractor(TelemetryConfiguration configuration);

        /// <summary>
        /// Implementations must override this method to actually extract metrics.
        /// This base class will catch and log all exceptions thrown here and it will tag all processed telemetry as required.
        /// </summary>
        /// <param name="fromItem">The item from which to extract metrics.</param>
        /// <param name="isItemProcessed">Whether the specified item was processed (or ignored) by this extractor.
        /// This determines whether the specified item will be tagged accordingly by adding the
        /// string "<c>(Name: {ExtractorName}, Ver:{ExtractorVersion})</c>" to the <c>xxx.ProcessedByExtractors></c> property.</param>
        public abstract void ExtractMetrics(ITelemetry fromItem, out bool isItemProcessed);

        /// <summary>
        /// Disposes this telemetry extractor. Subclasses that wish to dispose of additional resources should override not this,
        /// but the <see cref="Dispose(bool)"/> method, as described by the "Dispose Pattern".
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Disposes this telemetry extractor.
        /// Subclasses that wish to dispose of additional resources should override this method and call this base
        /// implementation as described by the "Dispose Pattern".
        /// </summary>
        /// <param name="disposing">Disposing or finalizing.</param>
        protected virtual void Dispose(bool disposing)
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
        /// All telemetry that has been processed by this extractor will be tagged by adding the
        /// string "<c>(Name: {ExtractorName}, Ver:{ExtractorVersion})</c>" to the <c>xxx.ProcessedByExtractors</c> property.
        /// This method constructs and caches that string.
        /// </summary>
        /// <returns>The formatted extractor info string.</returns>
        private string GetExtractorInfo()
        {
            string thisExtractorInfo = this.extractorInfo;

            if (thisExtractorInfo != null)
            {
                return thisExtractorInfo;
            }

            string extractorName = this.ExtractorName ?? "null";
            string extractorVersion = this.ExtractorVersion ?? "null";

            thisExtractorInfo = string.Format(
                                        CultureInfo.InvariantCulture,
                                        MetricTerms.Extraction.ProcessedByExtractors.Moniker.ExtractorInfoTemplate,
                                        extractorName,
                                        extractorVersion);

            // benign race:
            this.extractorInfo = thisExtractorInfo;
            return this.extractorInfo;
        }

        /// <summary>
        /// All telemetry that has been processed by this extractor will be tagged by adding the
        /// string "<c>(Name: {ExtractorName}, Ver:{ExtractorVersion})</c>" to the <c>xxx.ProcessedByExtractors</c> property.
        /// This method adds that string to the specified telemetry item's properties.
        /// </summary>
        /// <param name="item">The telemetry item to be tagged.</param>
        private void AddExtractorInfo(ITelemetry item)
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

            string thisExtractorInfo = this.GetExtractorInfo();
            extractionPipelineInfo = extractionPipelineInfo + thisExtractorInfo;
            item.Context.Properties[MetricTerms.Extraction.ProcessedByExtractors.Moniker.Key] = extractionPipelineInfo;
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
    }
}
