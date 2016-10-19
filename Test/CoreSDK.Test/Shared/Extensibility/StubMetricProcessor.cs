namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;

    internal class StubMetricProcessor : IMetricProcessor
    {
        private IList<MetricSample> sampleList;

        public StubMetricProcessor(IList<MetricSample> sampleList)
        {
            if (sampleList == null)
            {
                throw new ArgumentNullException("sampleList");
            }

            this.sampleList = sampleList;
        }

        public void Track(string metricName, double value, IDictionary<string, string> dimensions = null)
        {
            this.sampleList.Add(
                new MetricSample()
                {
                    MetricName = metricName,
                    Dimensions = dimensions,
                    Value = value
                });
        }
    }
}
