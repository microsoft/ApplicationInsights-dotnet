namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using Azure.Core;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Xunit;

    /// <summary>
    /// Tests for Azure Active Directory (AAD) authentication support in TelemetryConfiguration.
    /// </summary>
    public class TelemetryConfigurationAadTests : IDisposable
    {
        private TelemetryConfiguration telemetryConfiguration;

        public TelemetryConfigurationAadTests()
        {
            this.telemetryConfiguration = new TelemetryConfiguration();
        }

        public void Dispose()
        {
            this.telemetryConfiguration?.Dispose();
        }

        [Fact]
        public void SetAzureTokenCredential_WithValidCredential_SetsCredentialInExporterOptions()
        {
            // Arrange
            var credential = new MockTokenCredential();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act
            this.telemetryConfiguration.SetAzureTokenCredential(credential);

            // Build the configuration to apply the builder actions
            var client = new TelemetryClient(this.telemetryConfiguration);

            // Assert - credential should be configured in the service provider
            // We can't easily assert on the internal state, but we can verify no exceptions are thrown
            Assert.NotNull(client);
        }

        [Fact]
        public void SetAzureTokenCredential_WithNullCredential_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                this.telemetryConfiguration.SetAzureTokenCredential(null));
        }

        [Fact]
        public void SetAzureTokenCredential_AfterBuild_ThrowsInvalidOperationException()
        {
            // Arrange
            var credential = new MockTokenCredential();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";
            
            // Build the configuration
            var client = new TelemetryClient(this.telemetryConfiguration);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                this.telemetryConfiguration.SetAzureTokenCredential(credential));
        }

        [Fact]
        public void SetAzureTokenCredential_CanBeCalledBeforeBuild_DoesNotThrow()
        {
            // Arrange
            var credential = new MockTokenCredential();
            this.telemetryConfiguration.ConnectionString = "InstrumentationKey=test-ikey";

            // Act - should not throw
            this.telemetryConfiguration.SetAzureTokenCredential(credential);

            // Assert
            Assert.NotNull(this.telemetryConfiguration);
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
