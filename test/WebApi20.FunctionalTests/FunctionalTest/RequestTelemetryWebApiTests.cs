namespace WebApi20.FunctionalTests.FunctionalTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    using FunctionalTestUtils;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Xunit;
    using Xunit.Abstractions;

    public class RequestTelemetryWebApiTests : TelemetryTestsBase, IDisposable
    {
        private const string assemblyName = "WebApi20.FunctionalTests20";
        public RequestTelemetryWebApiTests(ITestOutputHelper output) : base (output)
        {
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingValuesController()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                const string RequestPath = "/api/values";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingNotExistingController()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                const string RequestPath = "/api/notexistingcontroller";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET /api/notexistingcontroller";
                expectedRequestTelemetry.ResponseCode = "404";
                expectedRequestTelemetry.Success = false;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingWebApiShimRoute()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                const string RequestPath = "/api/values/1";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get [id]";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestNoHeaderInjectionRequestTrackingOptions()
        {
            IWebHostBuilder Config(IWebHostBuilder builder)
            {
                return builder.ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetry(options => { options.RequestCollectionOptions.InjectResponseHeaders = false; });
                });
            }

            using (var server = new InProcessServer(assemblyName, this.output, Config))
            {
                const string RequestPath = "/api/values/1";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get [id]";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry, false);
            }
        }

        [Fact]
        public void TestW3COperationIdFormatGeneration()
        {
            IWebHostBuilder Config(IWebHostBuilder builder)
            {
                // disable Dependency tracking (i.e. header injection)
                return builder.ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetry();
                });
            }

            using (var server = new InProcessServer(assemblyName, this.output, Config))
            {
                const string RequestPath = "/api/values/1";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get [id]";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);

                var item = this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry, true);

                // W3C compatible-Id ( should go away when W3C is implemented in .NET https://github.com/dotnet/corefx/issues/30331)
                Assert.Equal(32, item.tags["ai.operation.id"].Length);
                Assert.True(Regex.Match(item.tags["ai.operation.id"], @"[a-z][0-9]").Success);
                // end of workaround test
            }
        }

        [Fact]
        public void TestW3CHeadersAreNotEnabledByDefault()
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                const string RequestPath = "/api/values";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new Uri(server.BaseHost + RequestPath);

                var activity = new Activity("dummy").SetParentId("|abc.123.").Start();
                var headers = new Dictionary<string, string>
                {
                    ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                    ["tracestate"] = "some=state"
                };

                var actualRequest = this.ValidateRequestWithHeaders(server, RequestPath, headers, expectedRequestTelemetry);

                Assert.Equal(activity.RootId, actualRequest.tags["ai.operation.id"]);
                Assert.Contains(activity.Id, actualRequest.tags["ai.operation.parentId"]);
            }
        }

        [Fact]
        public void TestW3CHeadersAreParsedWhenEnabledInConfig()
        {
            using (var server = new InProcessServer(assemblyName, this.output, builder =>
            {
                return builder.ConfigureServices( services =>
                {
                    services.AddApplicationInsightsTelemetry(o => o.RequestCollectionOptions.EnableW3CDistributedTracing = true);
                });
            }))
            {
                const string RequestPath = "/api/values";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new Uri(server.BaseHost + RequestPath);

                var activity = new Activity("dummy").SetParentId("|abc.123.").Start();
                var headers = new Dictionary<string, string>
                {
                    ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                    ["tracestate"] = "some=state",
                    ["Correlation-Context"] = "k1=v1,k2=v2"
                };

                var actualRequest = this.ValidateRequestWithHeaders(server, RequestPath, headers, expectedRequestTelemetry);

                Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", actualRequest.tags["ai.operation.id"]);
                Assert.Equal("|4bf92f3577b34da6a3ce929d0e0e4736.00f067aa0ba902b7.", actualRequest.tags["ai.operation.parentId"]);
                Assert.Equal("v1", actualRequest.data.baseData.properties["k1"]);
                Assert.Equal("v2", actualRequest.data.baseData.properties["k2"]);
            }
        }

        [Fact]
        public void TestW3CEnabledW3CHeadersOnly()
        {
            using (var server = new InProcessServer(assemblyName, this.output, builder =>
            {
                return builder.ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetry(o => o.RequestCollectionOptions.EnableW3CDistributedTracing = true);
                });
            }))
            {
                const string RequestPath = "/api/values";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new Uri(server.BaseHost + RequestPath);

                var headers = new Dictionary<string, string>
                {
                    ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                    ["tracestate"] = "some=state,az=cid-v1:xyz",
                    ["Correlation-Context"] = "k1=v1,k2=v2"
                };

                var actualRequest = this.ValidateRequestWithHeaders(server, RequestPath, headers, expectedRequestTelemetry);

                Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", actualRequest.tags["ai.operation.id"]);
                Assert.StartsWith("|4bf92f3577b34da6a3ce929d0e0e4736.00f067aa0ba902b7.", actualRequest.tags["ai.operation.parentId"]);
                Assert.Equal("v1", actualRequest.data.baseData.properties["k1"]);
                Assert.Equal("v2", actualRequest.data.baseData.properties["k2"]);
            }
        }

        [Fact]
        public void TestW3CEnabledRequestIdAndW3CHeaders()
        {
            using (var server = new InProcessServer(assemblyName, this.output, builder =>
            {
                return builder.ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetry(o => o.RequestCollectionOptions.EnableW3CDistributedTracing = true);
                });
            }))
            {
                const string RequestPath = "/api/values";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new Uri(server.BaseHost + RequestPath);

                // this will force Request-Id header injection, it will start with |abc.123.
                var activity = new Activity("dummy").SetParentId("|abc.123.").Start();
                var headers = new Dictionary<string, string>
                {
                    ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                    ["tracestate"] = "some=state,az=cid-v1:xyz",
                    ["Correlation-Context"] = "k1=v1,k2=v2"
                };

                var actualRequest = this.ValidateRequestWithHeaders(server, RequestPath, headers, expectedRequestTelemetry);

                Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", actualRequest.tags["ai.operation.id"]);
                Assert.StartsWith("|4bf92f3577b34da6a3ce929d0e0e4736.00f067aa0ba902b7.", actualRequest.tags["ai.operation.parentId"]);
                Assert.Equal("v1", actualRequest.data.baseData.properties["k1"]);
                Assert.Equal("v2", actualRequest.data.baseData.properties["k2"]);
                Assert.Equal("abc", actualRequest.data.baseData.properties["ai_legacyRootId"]);
                Assert.StartsWith("|abc.123", actualRequest.data.baseData.properties["ai_legacyRequestId"]);
            }
        }

        [Fact]
        public void TestW3CEnabledRequestIdAndNoW3CHeaders()
        {
            using (var server = new InProcessServer(assemblyName, this.output,
                builder =>
                {
                    return builder.ConfigureServices(services =>
                    {
                        services.AddApplicationInsightsTelemetry(o => o.RequestCollectionOptions.EnableW3CDistributedTracing = true);
                        services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((m, o) =>
                        {
                            // no correlation headers so we can test request
                            // call without auto-injected w3c headers
                            m.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("localhost");
                        });
                    });
                }))
            {
                const string RequestPath = "/api/values";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new Uri(server.BaseHost + RequestPath);

                // this will force Request-Id header injection, it will start with |abc.123.
                var activity = new Activity("dummy").SetParentId("|abc.123.").Start();
                var actualRequest = this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);

                Assert.Equal(32, actualRequest.tags["ai.operation.id"].Length);
                Assert.StartsWith("|abc.123.", actualRequest.tags["ai.operation.parentId"]);
            }
        }

        [Fact]
        public void TestW3CIsUsedWithoutHeadersWhenEnabledInConfig()
        {
            using (var server = new InProcessServer(assemblyName, this.output,
                builder =>
                {
                    return builder.ConfigureServices(services =>
                    {
                        services.AddApplicationInsightsTelemetry(o => o.RequestCollectionOptions.EnableW3CDistributedTracing = true);
                    });
                }))
            {
                const string RequestPath = "/api/values";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.Name = "GET Values/Get";
                expectedRequestTelemetry.ResponseCode = "200";
                expectedRequestTelemetry.Success = true;
                expectedRequestTelemetry.Url = new Uri(server.BaseHost + RequestPath);

                var actualRequest = this.ValidateBasicRequest(server, RequestPath, expectedRequestTelemetry);

                Assert.Equal(32, actualRequest.tags["ai.operation.id"].Length);
                Assert.Equal(1 + 32 + 1 + 16 + 1, actualRequest.data.baseData.id.Length);
            }
        }

        public void Dispose()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }
    }
}

