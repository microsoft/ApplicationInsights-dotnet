using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    public class MetricConsumerRegistrationToken
    {
        public int ConsumerCollectionOffset { get; internal set; }
        public Guid VerificationCode { get; internal set; }
    }
}
