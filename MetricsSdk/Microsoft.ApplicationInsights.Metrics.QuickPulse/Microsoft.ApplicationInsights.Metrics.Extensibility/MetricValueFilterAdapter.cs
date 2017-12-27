using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    internal class MetricValueFilterAdapter : IMetricValueFilter
    {
        private readonly Func<object, double, bool> _filterFunctionDouble;
        private readonly Func<object, object, bool> _filterFunctionObject;

        public MetricValueFilterAdapter(Func<object, double, bool> filterFunctionDouble, Func<object, object, bool> filterFunctionObject)
        {
            if (filterFunctionDouble == null)
            {
                throw new ArgumentNullException(nameof(filterFunctionDouble));
            }

            if (filterFunctionObject == null)
            {
                throw new ArgumentNullException(nameof(filterFunctionObject));
            }

            _filterFunctionDouble = filterFunctionDouble;
            _filterFunctionObject = filterFunctionObject;
        }

        public bool WillConsume(MetricSeries dataSeries, double metricValue)
        {
            bool willConsume = _filterFunctionDouble(dataSeries, metricValue);
            return willConsume;
        }

        public bool WillConsume(MetricSeries dataSeries, object metricValue)
        {
            bool willConsume = _filterFunctionObject(dataSeries, metricValue);
            return willConsume;
        }
    }
}
