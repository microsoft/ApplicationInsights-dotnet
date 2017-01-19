using Microsoft.ApplicationInsights.AspNetCore.Logging;
using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Logging
{
    /// <summary>
    /// Tests for the Application Insights ILogger implementation.
    /// </summary>
    public class ApplicationInsightsLoggerTests
    {
        /// <summary>
        /// Tests that the SDK version is correctly set on the telemetry context when messages are logged to AI.
        /// </summary>
        [Fact]
        public void TestLoggerSetsSdkVersionOnLoggedTelemetry()
        {
            bool isCorrectVersion = false;
            TelemetryClient client = CommonMocks.MockTelemetryClient((t) =>
            {
                isCorrectVersion = t.Context.GetInternalContext().SdkVersion.StartsWith(SdkVersionUtils.VersionPrefix);
            });

            ILogger logger = new ApplicationInsightsLogger("test", client, (s, l) => { return true; });
            logger.LogTrace("This is a test.", new object[] { });
            Assert.True(isCorrectVersion);
        }
    }
}
