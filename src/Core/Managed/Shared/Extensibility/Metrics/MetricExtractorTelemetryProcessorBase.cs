using System;
using System.Globalization;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

namespace Microsoft.ApplicationInsights.Extensibility.Metrics
{
    public abstract class MetricExtractorTelemetryProcessorBase : ITelemetryProcessor, ITelemetryModule, IDisposable
    {
        private MetricManager _metricManager = null;
        private ITelemetryProcessor _nextProcessorInPipeline = null;
        private string _extractorName = null;

        public MetricExtractorTelemetryProcessorBase(ITelemetryProcessor nextProcessorInPipeline)
            : this(nextProcessorInPipeline, metricTelemetryMarkerKey: null, metricTelemetryMarkerValue: null)
        {
        }

        public MetricExtractorTelemetryProcessorBase(ITelemetryProcessor nextProcessorInPipeline, string metricTelemetryMarkerKey, string metricTelemetryMarkerValue)
        {
            _nextProcessorInPipeline = nextProcessorInPipeline;

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
            get { return _metricManager; }
        }

        protected virtual string ExtractorName
        {
            get
            {
                string name = _extractorName;
                if (name == null)
                {
                    name = this.GetType().FullName;
                    _extractorName = name;  // benign race
                }
                return name;
            }
        }

        void ITelemetryModule.Initialize(TelemetryConfiguration configuration)
        {
            TelemetryClient telemetryClient = (configuration == null)
                                                    ? new TelemetryClient()
                                                    : new TelemetryClient(configuration);
            if (MetricTelemetryMarkerKey != null)
            {
                telemetryClient.Context.Properties[MetricTelemetryMarkerKey] = MetricTelemetryMarkerValue;
            }
            
            _metricManager = new MetricManager(new TelemetryClient(configuration));

            Initialize(configuration);
        }

        void ITelemetryProcessor.Process(ITelemetry item)
        {
            if (item == null)
            {
                InvokeNextProcessor(item);
                return;
            }

            try
            {
                Process(item);
                AddExtractorInfo(item);
            }
            catch(Exception ex)
            {
                // Is this te right way to log erroes here?
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
            bool hasPrevInfo = item.Context.Properties.TryGetValue(MetricTerms.Extraction.PipelineInfo.PropertyKey, out extractionPipelineInfo);

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
            item.Context.Properties[MetricTerms.Extraction.PipelineInfo.PropertyKey] = extractionPipelineInfo;
        }

        private void InvokeNextProcessor(ITelemetry item)
        {
            ITelemetryProcessor next = _nextProcessorInPipeline;
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
            IDisposable metricManager = _metricManager;
            if (metricManager != null)
            {
                // benign race
                metricManager.Dispose();
                _metricManager = null;
            }
        }

        protected abstract string ExtractorVersion { get; }
        public abstract void Initialize(TelemetryConfiguration configuration);
        public abstract void Process(ITelemetry item);
    }
}
