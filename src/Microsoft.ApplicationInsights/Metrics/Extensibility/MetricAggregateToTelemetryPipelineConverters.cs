namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>@ToDo: Complete documentation before stable release. {573}</summary>
    /// @PublicExposureCandidate
    internal sealed class MetricAggregateToTelemetryPipelineConverters 
    {
        /// <summary>@ToDo: Complete documentation before stable release. {097}</summary>
        public static readonly MetricAggregateToTelemetryPipelineConverters Registry = new MetricAggregateToTelemetryPipelineConverters();

        private ConcurrentDictionary<Type, ConcurrentDictionary<string, IMetricAggregateToTelemetryPipelineConverter>> pipelineTable
                                                        = new ConcurrentDictionary<Type, ConcurrentDictionary<string, IMetricAggregateToTelemetryPipelineConverter>>();

        /// <summary>@ToDo: Complete documentation before stable release. {109}</summary>
        /// <param name="pipelineType">@ToDo: Complete documentation before stable release. {517}</param>
        /// <param name="aggregationKindMoniker">@ToDo: Complete documentation before stable release. {274}</param>
        /// <param name="converter">@ToDo: Complete documentation before stable release. {912}</param>
        public void Add(Type pipelineType, string aggregationKindMoniker, IMetricAggregateToTelemetryPipelineConverter converter)
        {
            ValidateKeys(pipelineType, aggregationKindMoniker);
            Util.ValidateNotNull(converter, nameof(converter));

            ConcurrentDictionary<string, IMetricAggregateToTelemetryPipelineConverter> converters = this.pipelineTable.GetOrAdd(
                                                                                pipelineType,
                                                                                new ConcurrentDictionary<string, IMetricAggregateToTelemetryPipelineConverter>());

            converters[aggregationKindMoniker] = converter;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {076}</summary>
        /// <param name="pipelineType">@ToDo: Complete documentation before stable release. {807}</param>
        /// <param name="aggregationKindMoniker">@ToDo: Complete documentation before stable release. {677}</param>
        /// <param name="converter">@ToDo: Complete documentation before stable release. {420}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {143}</returns>
        public bool TryGet(Type pipelineType, string aggregationKindMoniker, out IMetricAggregateToTelemetryPipelineConverter converter)
        {
            ValidateKeys(pipelineType, aggregationKindMoniker);

            ConcurrentDictionary<string, IMetricAggregateToTelemetryPipelineConverter> converters;
            if (false == this.pipelineTable.TryGetValue(pipelineType, out converters))
            {
                converter = null;
                return false;
            }

            bool hasConverter = converters.TryGetValue(aggregationKindMoniker, out converter);
            return hasConverter;
        }

        private static void ValidateKeys(Type pipelineType, string aggregationKindMoniker)
        {
            Util.ValidateNotNull(pipelineType, nameof(pipelineType));

            ////if (false == typeof(IMetricTelemetryPipeline).IsAssignableFrom(pipelineType))
            ////{
            ////    throw new ArgumentException($"{nameof(pipelineType)} must specify a type that implements the interface '{typeof(IMetricTelemetryPipeline).Name}'"
            ////                              + $", but it specifies the type '{pipelineType.Name}' that does not implement that interface.");
            ////}

            Util.ValidateNotNullOrWhitespace(aggregationKindMoniker, nameof(aggregationKindMoniker));
        }
    }
}
