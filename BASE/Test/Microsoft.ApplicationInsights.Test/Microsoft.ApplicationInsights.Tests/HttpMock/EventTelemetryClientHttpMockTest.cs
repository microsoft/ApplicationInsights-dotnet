using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.ApplicationInsights;

[TestClass]
public class EventTelemetryClientHttpMockTest : AbstractTelemetryClientHttpMockTest
{
    
    [TestMethod]
    public async Task TrackEvent()
    {
        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackEvent("TestEvent");
        var expectedJson = SelectExpectedJson("event/expected-event.json", "event/expected-event-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }

    [TestMethod]
    public async Task TrackEventWithProperties()
    {
        var properties = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackEvent("TestEventWithProperties", properties);

        var expectedJson = SelectExpectedJson("event/expected-event-with-properties.json", "event/expected-event-with-properties-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }
    
    [TestMethod]
    public async Task TrackEventWithPropertiesAndMetrics()
    {
        var properties = new Dictionary<string, string>
        {
            { "Category", "UserActions" },
            { "Source", "MobileApp" }
        };

        var metrics = new Dictionary<string, double>
        {
            { "LoadTime", 1.42 },
            { "ClickCount", 17 }
        };

        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackEvent(
            "EventWithPropertiesAndMetrics",
            properties, metrics);

        await VerifyTrackMethod(ClientConsumer, "event/expected-event-with-properties-and-metrics.json");
    }
    
    [TestMethod]
    public async Task TrackEventWithNullTelemetryObject()
    {
        EventTelemetry noEvent = null;

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackEvent(noEvent);

        await VerifyTrackMethod(ClientConsumer, "event/expected-event-null-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackEventWithSimpleEventTelemetryObject()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
        {
            var anEventTelemetry = new EventTelemetry("SimpleTestEvent");
            telemetryClient.TrackEvent(anEventTelemetry);
        }

        await VerifyTrackMethod(ClientConsumer, "event/expected-event-with-simple-event-telemetry-object.json");
    }
    
    [TestMethod]
    public async Task TrackEventWithEventTelemetryObject
        ()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
            => telemetryClient.TrackEvent(AnEventTelemetry());

        await VerifyTrackMethod(ClientConsumer, "event/expected-event-with-event-telemetry-object.json",
            InternalNodeNameShouldBeProvidedInTags, NodeNameShouldBeEqualTo(NodeName));
    }
    
    private static EventTelemetry AnEventTelemetry()
    {
        var eventTelemetry = new EventTelemetry
        {
            Name = "SampleEvent",
            Timestamp = DateTimeOffset.UtcNow,
            Sequence = "sequence123"
        };

        eventTelemetry.Properties["Category"] = "UserActions";
        eventTelemetry.Properties["Source"] = "MobileApp";

        eventTelemetry.Metrics["LoadTime"] = 1.42;
        eventTelemetry.Metrics["ClickCount"] = 17;

        eventTelemetry.Context.User.Id = "user-001";
        eventTelemetry.Context.User.AccountId = "account-abc";
        eventTelemetry.Context.Session.Id = "session-xyz";
        eventTelemetry.Context.Device.Id = "device-555";
        eventTelemetry.Context.Device.Model = "Pixel 8";
        eventTelemetry.Context.Device.Type = "Phone";
        eventTelemetry.Context.Device.OemName = "Google";
        eventTelemetry.Context.Device.OperatingSystem = "Android 14";
        eventTelemetry.Context.Device.Language = "en-US";
        eventTelemetry.Context.Location.Ip = "192.0.2.1";
        eventTelemetry.Context.Cloud.RoleName = "api";
        eventTelemetry.Context.Cloud.RoleInstance = "instance-01";
        eventTelemetry.Context.Component.Version = "2.1.0";
        eventTelemetry.Context.Operation.Id = "op-req-01";
        eventTelemetry.Context.Operation.Name = "SampleOperation";
        eventTelemetry.Context.Operation.ParentId = "op-root-01";
        eventTelemetry.Context.Operation.CorrelationVector = "cvector-001";
        eventTelemetry.Context.InstrumentationKey = "00000000-0000-0000-0000-000000000000";
        eventTelemetry.Context.Internal.NodeName = NodeName;
        eventTelemetry.Context.Internal.SdkVersion = "dotnet:2.18.0";
        return eventTelemetry;
    }
    
    private static void InternalNodeNameShouldBeProvidedInTags(JObject currentJson)
    {
        var tags = currentJson["tags"] as JObject;

        if (tags == null)
            throw new AssertFailedException("Should have tags property.");

        Assert.IsFalse(string.IsNullOrEmpty(tags["ai.internal.nodeName"]?.ToString()),
            "tags should contain ai.internal.nodeName");
    }
}