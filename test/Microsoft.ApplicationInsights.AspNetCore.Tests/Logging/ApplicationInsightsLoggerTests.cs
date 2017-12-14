using System;
using Microsoft.ApplicationInsights.AspNetCore.Logging;
using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
using Microsoft.ApplicationInsights.DataContracts;
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

            ILogger logger = new ApplicationInsightsLogger("test", client, (s, l) => { return true; }, new ApplicationInsightsLoggerOptions());
            logger.LogTrace("This is a test.", new object[] { });
            Assert.True(isCorrectVersion);
        }

        /// <summary>
        /// Tests that logging an exception results in tracking an <see cref="ExceptionTelemetry"/> instance.
        /// </summary>
        [Fact]
        public void TestLoggerCreatesExceptionTelemetryOnLoggedError()
        {
            TelemetryClient client = CommonMocks.MockTelemetryClient((t) =>
            {
                Assert.IsType<ExceptionTelemetry>(t);
                var exceptionTelemetry = (ExceptionTelemetry)t;
                Assert.Equal("Error: This is an error", exceptionTelemetry.Message);
                Assert.Equal("System.Exception: This is an error", exceptionTelemetry.Properties["Exception"]);
                Assert.Equal(SeverityLevel.Error, exceptionTelemetry.SeverityLevel);
            });

            ILogger logger = new ApplicationInsightsLogger("test", client, (s, l) => { return true; }, new ApplicationInsightsLoggerOptions());
            var exception = new Exception("This is an error");
            logger.LogError(0, exception, "Error: " + exception.Message);
        }

        /// <summary>
        /// Tests that logging an exception results in tracking a <see cref="TraceTelemetry"/> instance when ApplicationInsightsLoggerOptions.TrackExceptionsAsExceptionTelemetry is set to false.
        /// </summary>
        [Fact]
        public void TestLoggerCreatesTraceTelemetryOnLoggedErrorWhenTrackExceptionsAsExceptionTelemetryIsSetToFalse()
        {
            TelemetryClient client = CommonMocks.MockTelemetryClient((t) =>
            {
                Assert.IsType<TraceTelemetry>(t);
                var traceTelemetry = (TraceTelemetry)t;

                Assert.Equal("Error: This is an error", traceTelemetry.Message);
                Assert.Equal(SeverityLevel.Error, traceTelemetry.SeverityLevel);
            });

            ILogger logger = new ApplicationInsightsLogger("test", client, (s, l) => { return true; }, new ApplicationInsightsLoggerOptions { TrackExceptionsAsExceptionTelemetry = false });
            var exception = new Exception("This is an error");

            logger.LogError(0, exception, "Error: " + exception.Message);
        }

        /// <summary>
        /// Tests that an incorrectly constructed or uninitialized Application Insights ILogger does not throw exceptions.
        /// </summary>
        [Fact]
        public void TestUninitializedLoggerDoesNotThrowExceptions()
        {
            ILogger logger = new ApplicationInsightsLogger("test", null, null, new ApplicationInsightsLoggerOptions());
            logger.LogTrace("This won't do anything.", new object[] { });
        }
    }
}
