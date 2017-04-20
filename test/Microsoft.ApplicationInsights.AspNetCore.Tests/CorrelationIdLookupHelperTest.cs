namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using System.Globalization;
    using System.Threading.Tasks;
    using ApplicationInsights.Extensibility;
    using DiagnosticListeners;
    using Xunit;

    public class CorrelationIdLookupHelperTest
    {
        private const string TestInstrumentationKey = "11111111-2222-3333-4444-555555555555";

        [Fact]
        public void TryGetXComponentCorrelationIdShouldReturnAppIdWhenHit()
        {
            CorrelationIdLookupHelper target = new CorrelationIdLookupHelper((iKey) =>
            {
                return Task.FromResult(string.Format(CultureInfo.InvariantCulture, "AppId for {0}", iKey));
            });

            string actual = null;
            target.TryGetXComponentCorrelationId(TestInstrumentationKey, out actual);
            string expected = string.Format(CultureInfo.InvariantCulture, CorrelationIdLookupHelper.CorrelationIdFormat, "AppId for " + TestInstrumentationKey);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TryGetXComponentCorrelationIdShouldReturnEmptyWhenIKeyIsNull()
        {
            TelemetryConfiguration config = new TelemetryConfiguration(TestInstrumentationKey, new FakeTelemetryChannel()
            {
                EndpointAddress = "https://endpoint"
            });
            CorrelationIdLookupHelper target = new CorrelationIdLookupHelper(() => config);
            string result = "Not null value";
            target.TryGetXComponentCorrelationId(null, out result);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void TryGetXComponentCorrelationIdShouldReturnEmptyWhenBaseAddressIsNotGiven()
        {
            // CorrelationIdLookupHelper should fail gracefully when it can't fetch the base address from the channel.
            TelemetryConfiguration config = new TelemetryConfiguration(TestInstrumentationKey, new FakeTelemetryChannel()
            {
                EndpointAddress = string.Empty
            });
            CorrelationIdLookupHelper target = new CorrelationIdLookupHelper(() => config);
            string result = "Not null value";
            target.TryGetXComponentCorrelationId(null, out result);

            Assert.Equal(string.Empty, result);
        }
    }
}
