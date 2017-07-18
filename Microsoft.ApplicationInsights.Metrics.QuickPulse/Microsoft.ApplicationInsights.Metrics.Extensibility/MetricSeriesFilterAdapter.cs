using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    internal class MetricSeriesFilterAdapter : IMetricSeriesFilter
    {
        private readonly Func<Tuple<bool,
                                    Tuple<Func<object, uint, bool>,
                                          Func<object, double, bool>,
                                          Func<object, object, bool>>>> _filterFunction;

        public MetricSeriesFilterAdapter(Func<Tuple<bool,
                                                    Tuple<Func<object, uint, bool>,
                                                          Func<object, double, bool>,
                                                          Func<object, object, bool>>>> filterFunction)
        {
            if (filterFunction == null)
            {
                throw new ArgumentNullException(nameof(filterFunction));
            }

            _filterFunction = filterFunction;
        }

        public bool WillConsume(MetricSeries dataSeries, out IMetricValueFilter valueFilter)
        {
            Tuple<bool,
                  Tuple<Func<object, uint, bool>,
                        Func<object, double, bool>,
                        Func<object, object, bool>>> seriesFilterResult;

            seriesFilterResult = _filterFunction();

            if (false == seriesFilterResult.Item1)
            {
                valueFilter = null;
                return false;
            }

            if (seriesFilterResult.Item2 == null)
            {
                throw new InvalidOperationException("The filterFunction returned WillConsume=True, so a 3-Tuple with value filter functions was expected,"
                                                 + $" but the returned Tuple was null. (filterFunction's target type: {_filterFunction.Target?.GetType().FullName}.)");
            }

            valueFilter = new MetricValueFilterAdapter(seriesFilterResult.Item2.Item1,
                                                       seriesFilterResult.Item2.Item2,
                                                       seriesFilterResult.Item2.Item3);
            return true;
        }
    }
}
