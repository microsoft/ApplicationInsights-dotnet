namespace SampleWebAppIntegration.FunctionalTest
{
    using System;
    using System.Linq;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class ExceptionTelemetryTests : TelemetryTestsBase
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
