namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils.Tests;
    using Microsoft.ApplicationInsights.DataContracts;
    using System;
    using System.Linq;
    using Xunit;

    public class ExceptionTelemetryTests : RequestTelemetryTestsBase
    {
        public ExceptionTelemetryTests() : base("Mvc6Framework45.FunctionalTests")
        { }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingControllerThatThrows()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.HttpMethod = "GET";
            expectedRequestTelemetry.Name = "GET Home/Exception";
            expectedRequestTelemetry.ResponseCode = "500";
            expectedRequestTelemetry.Success = false;
            // expectedRequestTelemetry.Url ???

            this.ValidateBasicRequest("/Home/Exception", expectedRequestTelemetry);
        }

        [Fact]
        public void TestBasicExceptionPropertiesAfterRequestingControllerThatThrows()
        {
            var expectedExceptionTelemetry = new ExceptionTelemetry();
            expectedExceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
            expectedExceptionTelemetry.Exception = new InvalidOperationException();

            this.ValidateBasicException("/Home/Exception", expectedExceptionTelemetry);
        }
    }
}
