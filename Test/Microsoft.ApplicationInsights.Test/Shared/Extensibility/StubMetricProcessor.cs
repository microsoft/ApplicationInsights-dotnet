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

        public void Track(Metric metric, double value)
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
