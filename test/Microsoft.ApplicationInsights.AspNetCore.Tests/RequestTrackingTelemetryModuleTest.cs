using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Xunit;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    public class RequestTrackingTelemetryModuleTest
    {
        [Fact]
        public void RequestTrackingTelemetryModuleDoesNoThrowWhenAppIdProviderisNull()
        {
            RequestTrackingTelemetryModule requestTrackingTelemetryModule = new RequestTrackingTelemetryModule(null);  
        }

        [Fact]
        public void RequestTrackingTelemetryModuleDoesNoThrowIfInitializeAfterDispose()
        {
            RequestTrackingTelemetryModule requestTrackingTelemetryModule = new RequestTrackingTelemetryModule(null);            
            requestTrackingTelemetryModule.Dispose();
            requestTrackingTelemetryModule.Initialize(new TelemetryConfiguration());
        }
    }
}
