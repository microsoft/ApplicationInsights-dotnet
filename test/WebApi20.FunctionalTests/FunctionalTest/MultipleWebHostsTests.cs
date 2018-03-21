using System.Linq;
using System.Runtime.InteropServices;
using AI;
using FunctionalTestUtils;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Xunit;
using Xunit.Abstractions;

namespace WebApi20.FunctionalTests20.FunctionalTest
{
    public class MultipleWebHostsTests : TelemetryTestsBase
    {
        private const string assemblyName = "WebApi20.FunctionalTests20";
        private const string requestPath = "/api/dependency";
        
        public MultipleWebHostsTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TwoWebHostsCreatedSequentially()
        {
            using (var server1 = new InProcessServer(assemblyName, this.output))
            {
                this.ExecuteRequest(server1.BaseHost + requestPath);
                var telemetry = server1.Listener.ReceiveItems(TestListenerTimeoutInMs);
                this.DebugTelemetryItems(telemetry);

                Assert.Single(telemetry.Where(t => t is TelemetryItem<RequestData>));
                Assert.Single(telemetry.Where(IsServiceDependencyCall));
                Assert.DoesNotContain(telemetry, t => t is TelemetryItem<ExceptionData>);

                var request = telemetry.Single(t => t is TelemetryItem<RequestData>);
                Assert.Equal("200", ((TelemetryItem<RequestData>)request).data.baseData.responseCode);
            }

            using (var server2 = new InProcessServer(assemblyName, this.output))
            {
                this.ExecuteRequest(server2.BaseHost + requestPath);
                var telemetry = server2.Listener.ReceiveItems(TestListenerTimeoutInMs);
                this.DebugTelemetryItems(telemetry);

                Assert.Single(telemetry.Where(t => t is TelemetryItem<RequestData>));
                Assert.Single(telemetry.Where(IsServiceDependencyCall));
                Assert.DoesNotContain(telemetry, t => t is TelemetryItem<ExceptionData>);

                var request = telemetry.Single(t => t is TelemetryItem<RequestData>);
                Assert.Equal("200", ((TelemetryItem<RequestData>)request).data.baseData.responseCode);
            }
        }

        [Fact(Skip = "We track each request and depednency by each WebHost, issue #621")]
        public void TwoWebHostsCreatedInParallel()
        {
            using (var server1 = new InProcessServer(assemblyName, this.output))
            using (var server2 = new InProcessServer(assemblyName, this.output))
            {
                this.ExecuteRequest(server1.BaseHost + requestPath);
                var telemetry1 = server1.Listener.ReceiveItems(TestListenerTimeoutInMs);
                this.DebugTelemetryItems(telemetry1);

                this.ExecuteRequest(server2.BaseHost + requestPath);
                var telemetry2 = server2.Listener.ReceiveItems(TestListenerTimeoutInMs);

                this.DebugTelemetryItems(telemetry2);
                Assert.Single(telemetry1.Where(t => t is TelemetryItem<RequestData>));
                Assert.Single(telemetry1.Where(IsServiceDependencyCall));
                Assert.DoesNotContain(telemetry1, t => t is TelemetryItem<ExceptionData>);

                var request1 = telemetry1.Single(t => t is TelemetryItem<RequestData>);
                Assert.Equal("200", ((TelemetryItem<RequestData>)request1).data.baseData.responseCode);

                // Fails here, we track everything twice
                // it did not happen with the first host because second one has not been really started yet
                Assert.Single(telemetry2.Where(t => t is TelemetryItem<RequestData>));
                Assert.Single(telemetry2.Where(IsServiceDependencyCall));
                Assert.DoesNotContain(telemetry2, t => t is TelemetryItem<ExceptionData>);

                var request2 = telemetry2.Single(t => t is TelemetryItem<RequestData>);
                Assert.Equal("200", ((TelemetryItem<RequestData>)request2).data.baseData.responseCode);
            }
        }

        [Fact]
        public void TwoWebHostsOneIsDisposed()
        {
            using (var server1 = new InProcessServer(assemblyName, this.output))
            {
                var config1 = (TelemetryConfiguration) server1.ApplicationServices.GetService(typeof(TelemetryConfiguration));
                this.ExecuteRequest(server1.BaseHost);

                using (var server2 = new InProcessServer(assemblyName, this.output))
                {
                    var config2 = (TelemetryConfiguration) server2.ApplicationServices.GetService(typeof(TelemetryConfiguration));

                    Assert.NotEqual(config1, config2);
                    Assert.NotEqual(config1.TelemetryChannel, config2.TelemetryChannel);

                    this.ExecuteRequest(server2.BaseHost);
                }

                Assert.NotNull(config1.TelemetryChannel);

                this.ExecuteRequest(server1.BaseHost + requestPath);
                
                var telemetry = server1.Listener.ReceiveItems(TestListenerTimeoutInMs);
                this.DebugTelemetryItems(telemetry);

                Assert.NotEmpty(telemetry.Where(t => t is TelemetryItem<RequestData>));
                var request = telemetry.Single(IsValueControllerRequest);
                Assert.Equal("200", ((TelemetryItem<RequestData>) request).data.baseData.responseCode);

                Assert.DoesNotContain(telemetry, t => t is TelemetryItem<ExceptionData>);
                Assert.Single(telemetry.Where(IsServiceDependencyCall));
            }
        }

        [Fact]
        public void ActiveConfigurationIsNotCorruptedAfterWebHostIsDisposed()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // https://github.com/dotnet/corefx/issues/25016
                // HttpListener on Linux/MacOS cannot be reused on the same port until connection
                // is closed on the OS level. So we run this test on windows only.
                return;
            }

            var activeConfig = TelemetryConfiguration.Active;
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                this.ExecuteRequest(server.BaseHost + requestPath);

                // receive everything and clean up
                server.Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TestListenerTimeoutInMs);
            }

            Assert.NotNull(activeConfig.TelemetryChannel);

            using (var listener = new TelemetryHttpListenerObservable(activeConfig.TelemetryChannel.EndpointAddress))
            {
                listener.Start();

                var telemetryClient = new TelemetryClient(activeConfig);
                telemetryClient.TrackTrace("some message after web host is disposed");

                var message = listener.ReceiveItemsOfType<TelemetryItem<MessageData>>(1, TestListenerTimeoutInMs);
                Assert.Single(message);
                Assert.Equal("some message after web host is disposed", ((TelemetryItem<MessageData>)message.Single()).data.baseData.message);
            }
        }

        private bool IsServiceDependencyCall(Envelope item)
        {
            if (!(item is TelemetryItem<RemoteDependencyData> dependency))
            {
                return false;
            }

            var url = dependency.data.baseData.data;

            // check if it's not tracked call from service to the test and a not call to get appid
            return url.Contains("microsoft.com");
        }

        private bool IsValueControllerRequest(Envelope item)
        {
            if (!(item is TelemetryItem<RequestData> dependency))
            {
                return false;
            }

            var url = dependency.data.baseData.url;

            // check if it's not tracked call from service to the test and a not call to get appid
            return url.Contains(requestPath);
        }
    }
}
