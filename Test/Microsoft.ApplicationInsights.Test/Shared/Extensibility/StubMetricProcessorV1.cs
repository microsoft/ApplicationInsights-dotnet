namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;

    internal class StubMetricProcessorV1 : IMetricProcessorV1
    {
        private IList<MetricSample> sampleList;

        public StubMetricProcessorV1(IList<MetricSample> sampleList)
        {
            if (sampleList == null)
            {
                throw new ArgumentNullException("sampleList");
            }

            this.sampleList = sampleList;
        }

        public void Track(MetricV1 metric, double value)
        {
            this.sampleList.Add(
                new MetricSample()
                {
                    Name = metric.Name,
                    Dimensions = metric.Dimensions,
                    Value = value
                });
        }
    }
}
