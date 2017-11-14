using System;
using System.Collections.Concurrent;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    public sealed class MetricAggregateToTelemetryPipelineConverters 
    {
        /// <summary />
        public static readonly MetricAggregateToTelemetryPipelineConverters Registry = new MetricAggregateToTelemetryPipelineConverters();

        private ConcurrentDictionary<Type, ConcurrentDictionary<string, IMetricAggregateToTelemetryPipelineConverter>> _pipelineTable
                                                        = new ConcurrentDictionary<Type, ConcurrentDictionary<string, IMetricAggregateToTelemetryPipelineConverter>>();

        /// <summary />
        /// <param name="pipelineType"></param>
        /// <param name="aggregationKindMoniker"></param>
        /// <param name="converter"></param>
        public void Add(Type pipelineType, string aggregationKindMoniker, IMetricAggregateToTelemetryPipelineConverter converter)
        {
            ValidateKeys(pipelineType, aggregationKindMoniker);
            Util.ValidateNotNull(converter, nameof(converter));

            ConcurrentDictionary<string, IMetricAggregateToTelemetryPipelineConverter> converters = _pipelineTable.GetOrAdd(
                                                                                pipelineType,
                                                                                new ConcurrentDictionary<string, IMetricAggregateToTelemetryPipelineConverter>());

            converters[aggregationKindMoniker] = converter;
        }

        /// <summary />
        /// <param name="pipelineType"></param>
        /// <param name="aggregationKindMoniker"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public bool TryGet(Type pipelineType, string aggregationKindMoniker, out IMetricAggregateToTelemetryPipelineConverter converter)
        {
            ValidateKeys(pipelineType, aggregationKindMoniker);

            ConcurrentDictionary<string, IMetricAggregateToTelemetryPipelineConverter> converters;
            if (false == _pipelineTable.TryGetValue(pipelineType, out converters))
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

            //if (false == typeof(IMetricTelemetryPipeline).IsAssignableFrom(pipelineType))
            //{
            //    throw new ArgumentException($"{nameof(pipelineType)} must specify a type that implements the interface '{typeof(IMetricTelemetryPipeline).Name}'"
            //                              + $", but it specifies the type '{pipelineType.Name}' that does not implement that interface.");
            //}

            Util.ValidateNotNullOrWhitespace(aggregationKindMoniker, nameof(aggregationKindMoniker));
        }
    }
}
