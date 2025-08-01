using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights;

[TestClass]
public class MetricTelemetryClientHttpMockTest : AbstractTelemetryClientHttpMockTest
{
    
    [TestMethod]
    public async Task TrackMetric()
    {
        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackMetric("testMetric", 23.56);

        await VerifyTrackMethod(ClientConsumer, "metric/expected-metric.json");
    }

    [TestMethod]
    public async Task TrackMetricWithProperties()
    {
        var properties = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackMetric("testMetricWithProperties", 37.26, properties);

        await VerifyTrackMethod(ClientConsumer, "metric/expected-metric-with-properties.json");
    }

    [TestMethod]
    public async Task TrackMetricWithNullTelemetryObject()
    {
        MetricTelemetry noMetric = null;

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackMetric(noMetric);

        await VerifyTrackMethod(ClientConsumer, "metric/expected-metric-with-null-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackMetricWithSimpleMetricTelemetryObject()
    {
        var simpleMetricTelemetry = new MetricTelemetry("testMetricWithSimpleMetricTelemetryObject", 12.249);

        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackMetric(simpleMetricTelemetry);

        await VerifyTrackMethod(ClientConsumer, "metric/expected-metric-with-simple-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackMetricWithTelemetryObject()
    {
        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackMetric(AMetricTelemetry());

        await VerifyTrackMethod(ClientConsumer, "metric/expected-metric-with-telemetry-object.json",
            NodeNameShouldBeEqualTo(NodeName));
    }
    
    private static MetricTelemetry AMetricTelemetry()
    {
        var metricTelemetry = new MetricTelemetry
        {
            Name = "CustomMetric",
            Sum = 123.45,
            Min = 120,
            Max = 130,
            Count = 5,
            StandardDeviation = 3.2,
            Timestamp = DateTimeOffset.UtcNow,
            Sequence = "seq-123"
        };

        metricTelemetry.Properties["Key1"] = "Value1";
        metricTelemetry.Properties["Key2"] = "Value2";

        metricTelemetry.Context.InstrumentationKey = "12345678-1234-1234-1234-123456789012";
        metricTelemetry.Context.Component.Version = "3.2.1";
        metricTelemetry.Context.User.Id = "user-xyz";
        metricTelemetry.Context.User.AccountId = "account-abcd";
        metricTelemetry.Context.Session.Id = "session-5555";
        metricTelemetry.Context.Device.Id = "device-007";
        metricTelemetry.Context.Device.Type = "Desktop";
        metricTelemetry.Context.Device.Model = "Precision";
        metricTelemetry.Context.Device.OperatingSystem = "Windows 11";
        metricTelemetry.Context.Device.OemName = "Dell";
        metricTelemetry.Context.Device.Language = "en-US";
        metricTelemetry.Context.Location.Ip = "198.51.100.5";
        metricTelemetry.Context.Cloud.RoleName = "backend";
        metricTelemetry.Context.Cloud.RoleInstance = "service01";
        metricTelemetry.Context.Operation.Id = "op-4321";
        metricTelemetry.Context.Operation.Name = "MetricOperation";
        metricTelemetry.Context.Operation.ParentId = "op-root";
        metricTelemetry.Context.Operation.CorrelationVector = "cv-0:a";
        metricTelemetry.Context.Internal.SdkVersion = "netcoreapp3.1:2.206.0";
        metricTelemetry.Context.Internal.NodeName = NodeName;
        return metricTelemetry;
    }
}