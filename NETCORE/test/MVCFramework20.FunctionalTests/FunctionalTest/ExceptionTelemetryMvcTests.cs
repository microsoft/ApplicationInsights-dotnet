using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace MVC20.FuncTests
{
    using System;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;
    using Xunit.Abstractions;

    public class ExceptionTelemetryMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework20.FunctionalTests20";

        public ExceptionTelemetryMvcTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingControllerThatThrows()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                const string RequestPath = "/Home/Exception";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Home/Exception";
                expectedRequestTelemetry.ResponseCode = "500";
                expectedRequestTelemetry.Success = false;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                // the is no response header because of https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/717
                this.ValidateBasicRequest(server, "/Home/Exception", expectedRequestTelemetry, false);
            }
        }

        [Fact]
        public void TestBasicExceptionPropertiesAfterRequestingControllerThatThrows()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                var expectedExceptionTelemetry = new ExceptionTelemetry();
                expectedExceptionTelemetry.Exception = new InvalidOperationException();

                this.ValidateBasicException(server, "/Home/Exception", expectedExceptionTelemetry);
            }
        }
    }
}
