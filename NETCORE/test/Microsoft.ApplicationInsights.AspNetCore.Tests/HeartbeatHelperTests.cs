using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
using Microsoft.ApplicationInsights.Shared.Implementation;
using Microsoft.ApplicationInsights.WindowsServer;

using Xunit;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    public class HeartbeatHelperTests
    {
        [Fact]
        public void VerifyCanSetAppServiceHeartbeatTelemetryModuleProperty()
        {
            var module = new AppServicesHeartbeatTelemetryModule();
            var diagnosticsTelemetryModule = new DiagnosticsTelemetryModule();

            Assert.Null(module.HeartbeatPropertyManager);
            HeartbeatHelper.SetHeartbeatPropertyManager(module, diagnosticsTelemetryModule);
            Assert.NotNull(module.HeartbeatPropertyManager);
            Assert.Equal(diagnosticsTelemetryModule, module.HeartbeatPropertyManager);
        }

        [Fact]
        public void VerifyCanSetAzureInstanceMetadataTelemetryModuleProperty()
        {
            var module = new AzureInstanceMetadataTelemetryModule();
            var diagnosticsTelemetryModule = new DiagnosticsTelemetryModule();

            Assert.Null(module.HeartbeatPropertyManager);
            HeartbeatHelper.SetHeartbeatPropertyManager(module, diagnosticsTelemetryModule);
            Assert.NotNull(module.HeartbeatPropertyManager);
            Assert.Equal(diagnosticsTelemetryModule, module.HeartbeatPropertyManager);
        }

        [Fact]
        public void VerifyCanSetAppServiceHeartbeatTelemetryModuleProperty_Null()
        {
            var module = new AppServicesHeartbeatTelemetryModule();

            Assert.Null(module.HeartbeatPropertyManager);
            HeartbeatHelper.SetHeartbeatPropertyManager(module, null);
            Assert.Null(module.HeartbeatPropertyManager);
        }

        [Fact]
        public void VerifyCanSetAzureInstanceMetadataTelemetryModuleProperty_Null()
        {
            var module = new AzureInstanceMetadataTelemetryModule();

            Assert.Null(module.HeartbeatPropertyManager);
            HeartbeatHelper.SetHeartbeatPropertyManager(module, null);
            Assert.Null(module.HeartbeatPropertyManager);
        }
    }
}
