namespace IntegrationTests.Tests.TestFramework
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

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
            //Assert.Equal(expectedUri, requestTelemetry.Url.ToString());
            Assert.Equal(expectedUri, requestTelemetry.Url);
            Assert.True(requestTelemetry.Duration.TotalMilliseconds > 0);
            // requestTelemetry.Timestamp
        }
    }
}
