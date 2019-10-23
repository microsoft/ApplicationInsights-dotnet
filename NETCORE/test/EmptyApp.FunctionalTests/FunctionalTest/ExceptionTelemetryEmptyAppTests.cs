using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace EmptyApp.FunctionalTests.FunctionalTest
{
    using System;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;

    public class ExceptionTelemetryEmptyAppTests : TelemetryTestsBase
    {
        private const string assemblyName = "EmptyApp.FunctionalTests";
        public ExceptionTelemetryEmptyAppTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingRequestThatThrows()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                const string RequestPath = "/Exception";

                var expectedRequestTelemetry = new RequestTelemetry();

                expectedRequestTelemetry.Name = "GET /Exception";
                expectedRequestTelemetry.ResponseCode = "500";
                expectedRequestTelemetry.Success = false;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);
                this.ValidateBasicRequest(server, "/Exception", expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicExceptionPropertiesAfterRequestingRequestThatThrows()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                var expectedExceptionTelemetry = new ExceptionTelemetry();
                expectedExceptionTelemetry.Exception = new InvalidOperationException();

                this.ValidateBasicException(server, "/Exception", expectedExceptionTelemetry);
            }
        }
    }
}
