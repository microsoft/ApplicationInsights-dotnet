using System;
using System.Globalization;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

namespace Microsoft.ApplicationInsights.Extensibility
{

    /// <summary>
    /// Participates in the telemetry pipeline as a telemetry processor and enables subclasses to extracts auto-collected, pre-aggregated
    /// metrics from telemetry objects unsing minimal effort.
    /// </summary>
    public abstract class MetricExtractorTelemetryProcessorBase : ITelemetryProcessor, ITelemetryModule, IDisposable
    {
        private MetricManager metricManager = null;
        private ITelemetryProcessor nextProcessorInPipeline = null;
        private string extractorName = null;

        public MetricExtractorTelemetryProcessorBase(ITelemetryProcessor nextProcessorInPipeline)
            : this(nextProcessorInPipeline, metricTelemetryMarkerKey: null, metricTelemetryMarkerValue: null)
        {
        }

        public MetricExtractorTelemetryProcessorBase(ITelemetryProcessor nextProcessorInPipeline,
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
                metricTelemetryMarkerValue = String.Empty;
            }

            MetricTelemetryMarkerKey = metricTelemetryMarkerKey;
            MetricTelemetryMarkerValue = metricTelemetryMarkerValue;
        }

        public string MetricTelemetryMarkerKey { get; private set; }

        public string MetricTelemetryMarkerValue { get; private set; }

        protected MetricManager MetricManager
        {
            get { return this.metricManager; }
        }

        protected virtual string ExtractorName
        {
            get
            {
                string name = this.extractorName;
                if (name == null)
                {
                    name = this.GetType().FullName;
                    this.extractorName = name;  // benign race
                }
                return name;
            }
        }

        public void Initialize(TelemetryConfiguration configuration)
        {
            TelemetryClient telemetryClient = (configuration == null)
                                                    ? new TelemetryClient()
                                                    : new TelemetryClient(configuration);
            if (MetricTelemetryMarkerKey != null)
            {
                telemetryClient.Context.Properties[MetricTelemetryMarkerKey] = MetricTelemetryMarkerValue;
            }

            this.metricManager = new MetricManager(telemetryClient);

            InitializeExtractor(configuration);
        }

        public void Process(ITelemetry item)
        {
            if (item == null)
            {
                InvokeNextProcessor(item);
                return;
            }

            try
            {
                bool isItemProcessed;
                ExtractMetrics(item, out isItemProcessed);

                if (isItemProcessed)
                {
                    AddExtractorInfo(item);
                }
            }
            catch(Exception ex)
            {
                CoreEventSource.Log.LogError("Error in Metric Extractor: " + ex.ToString());
            }

            InvokeNextProcessor(item);
        }

        private void AddExtractorInfo(ITelemetry item)
        {
            string extractorName = ExtractorName ?? "null";
            string extractorVersion = ExtractorVersion ?? "null";

            string thisExtractorInfo = String.Format(CultureInfo.InvariantCulture,
                                                     "(Name:{0}, Ver:{1})",
                                                     extractorName,
                                                     extractorVersion);

            string extractionPipelineInfo;
            bool hasPrevInfo = item.Context.Properties.TryGetValue(MetricTerms.Extraction.ProcessedByExtractors.Moniker.Key, out extractionPipelineInfo);

            if (! hasPrevInfo)
            {
                extractionPipelineInfo = String.Empty;
            }
            else
            {
                if (extractionPipelineInfo.Length > 0)
                {
                    extractionPipelineInfo = extractionPipelineInfo + "; ";
                }
            }

            extractionPipelineInfo = extractionPipelineInfo + thisExtractorInfo;
            item.Context.Properties[MetricTerms.Extraction.ProcessedByExtractors.Moniker.Key] = extractionPipelineInfo;
        }

        private void InvokeNextProcessor(ITelemetry item)
        {
            ITelemetryProcessor next = this.nextProcessorInPipeline;
            if (next != null)
            {
                next.Process(item);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

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

        protected abstract string ExtractorVersion { get; }
        public abstract void InitializeExtractor(TelemetryConfiguration configuration);
        public abstract void ExtractMetrics(ITelemetry fromItem, out bool isItemProcessed);
    }
}
