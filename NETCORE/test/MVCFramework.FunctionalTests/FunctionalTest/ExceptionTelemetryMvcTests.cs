using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace MVCFramework.FunctionalTests.FunctionalTest
{
    using System;
    using System.Reflection;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;
    using Xunit.Abstractions;

    public class ExceptionTelemetryMvcTests : TelemetryTestsBase
    {
        private readonly string assemblyName;

        public ExceptionTelemetryMvcTests(ITestOutputHelper output) : base(output)
        {
            this.assemblyName = this.GetType().GetTypeInfo().Assembly.GetName().Name;
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingControllerThatThrows()
        {
            using (var server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                const string RequestPath = "/Home/Exception";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Home/Exception";
                expectedRequestTelemetry.ResponseCode = "500";
                expectedRequestTelemetry.Success = false;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);
                this.ValidateBasicRequest(server, "/Home/Exception", expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicExceptionPropertiesAfterRequestingControllerThatThrows()
        {
            using (var server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                var expectedExceptionTelemetry = new ExceptionTelemetry();
                expectedExceptionTelemetry.Exception = new InvalidOperationException();

                this.ValidateBasicException(server, "/Home/Exception", expectedExceptionTelemetry);
            }
        }
    }
}
