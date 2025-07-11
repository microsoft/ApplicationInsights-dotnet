using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights;

[TestClass]
public class TraceTelemetryClientHttpMockTest : AbstractTelemetryClientHttpMockTest
{
    
    [TestMethod]
    public async Task TrackTrace()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace("Application Insights trace");

        var expectedJson = SelectExpectedJson("trace/expected-trace.json", "trace/expected-trace-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }
    
    [TestMethod]
    public async Task TrackTraceWithWarningSeverityLevel()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace("Warning message", SeverityLevel.Warning);

        var expectedJson = SelectExpectedJson("trace/expected-trace-with-warning-severity-level.json",
            "trace/expected-trace-with-warning-severity-level-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }

    
    [TestMethod]
    public async Task TrackTraceWithInformationSeverityLevel()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace("Information message", SeverityLevel.Information);

        var expectedJson = SelectExpectedJson("trace/expected-trace-with-information-severity-level.json",
            "trace/expected-trace-with-information-severity-level-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }
    
    [TestMethod]
    public async Task TrackTraceWithVerboseSeverityLevel()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace("Verbose message", SeverityLevel.Verbose);

        var expectedJson = SelectExpectedJson("trace/expected-trace-with-verbose-severity-level.json",
            "trace/expected-trace-with-verbose-severity-level-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }
    
    [TestMethod]
    public async Task TrackTraceWithErrorSeverityLevel()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace("Error message", SeverityLevel.Error);

        var expectedJson = SelectExpectedJson("trace/expected-trace-with-error-severity-level.json",
            "trace/expected-trace-with-error-severity-level-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }

    [TestMethod]
    public async Task TrackTraceWithCriticalSeverityLevel()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace("Critical message", SeverityLevel.Critical);

        var expectedJson = SelectExpectedJson("trace/expected-trace-with-critical-severity-level.json",
            "trace/expected-trace-with-critical-severity-level-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }
    
    [TestMethod]
    public async Task TrackTraceWithProperties()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
        {
            var properties = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };
            telemetryClient.TrackTrace("Application Insights trace",
                properties);
        }

        await VerifyTrackMethod(ClientConsumer, "trace/expected-trace-with-properties.json");
    }

    [TestMethod]
    public async Task TrackTraceWithSeverityLevelAndProperties()
    {
        var properties = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace("Application Insights trace", SeverityLevel.Error, properties);

        await VerifyTrackMethod(ClientConsumer, "trace/expected-trace-with-severity-level-and-properties.json");
    }
    
    [TestMethod]
    public async Task TrackTraceWithNullTelemetryObject()
    {
        TraceTelemetry noTrace = null;

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace(noTrace);

        await VerifyTrackMethod(ClientConsumer, "trace/expected-trace-null-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackTraceWithSimpleTraceTelemetryObject()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
        {
            var aTraceTelemetry = new TraceTelemetry("Application Insights trace", SeverityLevel.Critical);
            telemetryClient.TrackTrace(aTraceTelemetry);
        }

        await VerifyTrackMethod(ClientConsumer, "trace/expected-trace-with-simple-trace-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackTraceWithTraceTelemetryObject()
    {
        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackTrace(ATraceTelemetry());

        await VerifyTrackMethod(ClientConsumer, "trace/expected-trace-with-trace-telemetry-object.json");
    }
    
    private static TraceTelemetry ATraceTelemetry()
    {
        var traceTelemetry = new TraceTelemetry
        {
            Message = "Application Insights trace",
            SeverityLevel = SeverityLevel.Error,
            Timestamp = DateTimeOffset.UtcNow,
            Sequence = "seq-001"
        };

        traceTelemetry.Properties["Key1"] = "Value1";
        traceTelemetry.Properties["Key2"] = "Value2";
        traceTelemetry.Properties["environment"] = "test";

        traceTelemetry.Context.InstrumentationKey = "12345678-1234-1234-1234-123456789012";
        traceTelemetry.Context.Component.Version = "1.2.3";
        traceTelemetry.Context.User.Id = "user-xyz";
        traceTelemetry.Context.User.AccountId = "acc-abc";
        traceTelemetry.Context.Session.Id = "session-9876";
        traceTelemetry.Context.Device.Id = "device-42";
        traceTelemetry.Context.Device.Type = "PC";
        traceTelemetry.Context.Device.Model = "Surface Pro";
        traceTelemetry.Context.Device.OperatingSystem = "Windows 11";
        traceTelemetry.Context.Device.OemName = "Microsoft";
        traceTelemetry.Context.Device.Language = "en-US";
        traceTelemetry.Context.Location.Ip = "203.0.113.42";
        traceTelemetry.Context.Cloud.RoleName = "webapi";
        traceTelemetry.Context.Cloud.RoleInstance = "web01";
        traceTelemetry.Context.Operation.Id = "operation-001";
        traceTelemetry.Context.Operation.Name = "SampleOperation";
        traceTelemetry.Context.Operation.ParentId = "root-operation-id";
        traceTelemetry.Context.Operation.CorrelationVector = "cv-0:b";
        traceTelemetry.Context.Internal.SdkVersion = "customSdk-1.0.0";
        return traceTelemetry;
    }
}