namespace IntegrationTests.Tests.TestFramework
{
    using System;

    using Microsoft.ApplicationInsights.DataContracts;

    using Xunit;

    public static class TelemetryValidation
    {
        public static void ValidateRequest(RequestTelemetry requestTelemetry,
            string expectedResponseCode,
            string expectedName,
            Uri expectedUri,
            bool expectedSuccess)
        {
            Assert.Equal(expectedResponseCode, requestTelemetry.ResponseCode);
            Assert.Equal(expectedName, requestTelemetry.Name);
            Assert.Equal(expectedSuccess, requestTelemetry.Success);
            Assert.Equal(expectedUri, requestTelemetry.Url);
            Assert.True(requestTelemetry.Duration.TotalMilliseconds > 0);
            // requestTelemetry.Timestamp
        }
    }
}
