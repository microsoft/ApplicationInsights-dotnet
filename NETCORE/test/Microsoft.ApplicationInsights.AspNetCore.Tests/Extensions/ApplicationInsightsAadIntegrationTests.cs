namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Extensions
{
    using System;
    using System.Linq;
    using Azure.Core;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Xunit;

    /// <summary>
    /// Integration tests for Azure Active Directory (AAD) authentication support in AspNetCore.
    /// </summary>
    public class ApplicationInsightsAadIntegrationTests
    {
        /// <summary>
        /// Tests that credential flows from ApplicationInsightsServiceOptions to AzureMonitorExporterOptions.
        /// </summary>
        [Fact]
        public void AddApplicationInsightsTelemetry_WithCredential_FlowsCredentialToExporter()
        {
            // Arrange
            var services = new ServiceCollection();
            var credential = new MockTokenCredential();
            var connectionString = "InstrumentationKey=test-ikey;IngestionEndpoint=https://test.endpoint.com/";

            // Act
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = connectionString;
                options.Credential = credential;
            });

            var serviceProvider = services.BuildServiceProvider();

            // Get the configured AzureMonitorExporterOptions
            var exporterOptions = serviceProvider.GetService<IOptions<AzureMonitorExporterOptions>>();

            // Assert
            Assert.NotNull(exporterOptions);
            Assert.NotNull(exporterOptions.Value);
            Assert.Same(credential, exporterOptions.Value.Credential);
        }

        /// <summary>
        /// Tests that credential is null when not set.
        /// </summary>
        [Fact]
        public void AddApplicationInsightsTelemetry_WithoutCredential_HasNullCredentialInExporter()
        {
            // Arrange
            var services = new ServiceCollection();
            var connectionString = "InstrumentationKey=test-ikey;IngestionEndpoint=https://test.endpoint.com/";

            // Act
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = connectionString;
                // No credential set
            });

            var serviceProvider = services.BuildServiceProvider();

            // Get the configured AzureMonitorExporterOptions
            var exporterOptions = serviceProvider.GetService<IOptions<AzureMonitorExporterOptions>>();

            // Assert
            Assert.NotNull(exporterOptions);
            Assert.NotNull(exporterOptions.Value);
            Assert.Null(exporterOptions.Value.Credential);
        }

        /// <summary>
        /// Tests that TelemetryClient can be created with credential configured.
        /// </summary>
        [Fact]
        public void TelemetryClient_WithCredential_CreatesSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();
            var credential = new MockTokenCredential();
            var connectionString = "InstrumentationKey=test-ikey;IngestionEndpoint=https://test.endpoint.com/";

            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = connectionString;
                options.Credential = credential;
            });

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var telemetryClient = serviceProvider.GetService<TelemetryClient>();

            // Assert
            Assert.NotNull(telemetryClient);
            Assert.NotNull(telemetryClient.TelemetryConfiguration);
        }

        /// <summary>
        /// Mock TokenCredential for testing purposes.
        /// </summary>
        private class MockTokenCredential : TokenCredential
        {
            public override AccessToken GetToken(TokenRequestContext requestContext, System.Threading.CancellationToken cancellationToken)
            {
                return new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1));
            }

            public override System.Threading.Tasks.ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, System.Threading.CancellationToken cancellationToken)
            {
                return new System.Threading.Tasks.ValueTask<AccessToken>(
                    new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            }
        }
    }
}
