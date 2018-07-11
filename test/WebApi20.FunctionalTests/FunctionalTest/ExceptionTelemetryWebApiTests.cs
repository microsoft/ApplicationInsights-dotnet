using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace WebApi20.FunctionalTests.FunctionalTest
{
    using System;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit.Abstractions;

    public class ExceptionTelemetryWebApiTests : TelemetryTestsBase
    {
        private const string assemblyName = "WebApi20.FunctionalTests20";

        
        public ExceptionTelemetryWebApiTests(ITestOutputHelper output) : base (output)
        {            
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingControllerThatThrows()
        {            
            using (var server = new InProcessServer(assemblyName, this.output))
            {                
                const string RequestPath = "/api/exception";                

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Exception/Get";                
                expectedRequestTelemetry.ResponseCode = "500";
                expectedRequestTelemetry.Success = false;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                // the is no response header because of https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/717
                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry, false);
            }
        }

        [Fact]
        public void TestBasicExceptionPropertiesAfterRequestingControllerThatThrows()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                var expectedExceptionTelemetry = new ExceptionTelemetry();
                expectedExceptionTelemetry.Exception = new InvalidOperationException();

                this.ValidateBasicException(server, "/api/exception", expectedExceptionTelemetry);
            }
        }
    }
}
