using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights;

[TestClass]
public class RequestTelemetryClientHttpMockTest : AbstractTelemetryClientHttpMockTest
{
    [TestMethod]
    public async Task TrackRequest()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
        {
            telemetryClient.Context.Properties["Key1"] = "Value1";
            telemetryClient.Context.Properties["Key2"] = "Value2";
            telemetryClient.Context.GlobalProperties["global-Key1"] = "global-Value1";
            telemetryClient.Context.GlobalProperties["global-Key2"] = "global-Value2";
            telemetryClient.TrackRequest("GET /api/orders", DateTimeOffset.Now, TimeSpan.FromMilliseconds(123), "200",
                true);
        }

        var expectedJson = SelectExpectedJson("request/expected-request.json", "request/expected-request-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson, IdShouldBeProvidedInBaseData);
    }

    [TestMethod]
    public async Task TrackRequestWithNullTelemetryObject()
    {
        RequestTelemetry noRequest = null;

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackRequest(noRequest);

        await VerifyTrackMethod(ClientConsumer, "request/expected-request-with-null-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackRequestWithRequestTelemetryObject()
    {
        void ExceptionConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackRequest(ARequestTelemetry());

        await VerifyTrackMethod(ExceptionConsumer, "request/expected-request-with-request-telemetry-object.json");
    }
    
     private static RequestTelemetry ARequestTelemetry()
    {
        var requestTelemetry = new RequestTelemetry
        {
            Id = "req-12345",
            Name = "GET /api/orders",
            Timestamp = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(250),
            ResponseCode = "200",
            Success = true,
            Url = new Uri("https://api.example.com/api/orders"),
            Source = "frontend-service",
            Sequence = "seq-001",
            HttpMethod = "GET"
        };

        requestTelemetry.Properties["Key1"] = "Value1";
        requestTelemetry.Properties["Key2"] = "Value2";

        requestTelemetry.Metrics["Metric1"] = 42.0;
        requestTelemetry.Metrics["Metric2"] = 3.14;

        requestTelemetry.Context.User.Id = "user-001";
        requestTelemetry.Context.User.AccountId = "account-abc";
        requestTelemetry.Context.Session.Id = "session-xyz";
        requestTelemetry.Context.Device.Id = "device-555";
        requestTelemetry.Context.Device.Model = "Surface Pro";
        requestTelemetry.Context.Device.Type = "PC";
        requestTelemetry.Context.Device.OemName = "Microsoft";
        requestTelemetry.Context.Device.OperatingSystem = "Windows 11";
        requestTelemetry.Context.Device.Language = "fr-FR";
        requestTelemetry.Context.Location.Ip = "192.0.2.1";
        requestTelemetry.Context.Cloud.RoleName = "api";
        requestTelemetry.Context.Cloud.RoleInstance = "instance-01";
        requestTelemetry.Context.Component.Version = "2.1.0";
        requestTelemetry.Context.Operation.Id = "op-req-01";
        requestTelemetry.Context.Operation.Name = "SampleOperation";
        requestTelemetry.Context.Operation.ParentId = "op-root-01";
        requestTelemetry.Context.Operation.CorrelationVector = "cvector-001";
        requestTelemetry.Context.Internal.SdkVersion = "dotnet:2.18.0";
        requestTelemetry.Context.InstrumentationKey = "00000000-0000-0000-0000-000000000000";

        return requestTelemetry;
    }
}