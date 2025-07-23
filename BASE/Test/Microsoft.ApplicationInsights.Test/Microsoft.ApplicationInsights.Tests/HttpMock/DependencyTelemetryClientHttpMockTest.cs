using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights;

[TestClass]
public class DependencyTelemetryClientHttpMockTest  : AbstractTelemetryClientHttpMockTest
{
    
    [TestMethod]
    public async Task TrackDependencyObsolete()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackDependency("GetOrders", "SELECT * FROM Orders", DateTimeOffset.Now,
                TimeSpan.FromMilliseconds(123), true);
        
        var expectedJson = SelectExpectedJson("dependency/expected-dependency-obsolete.json", "dependency/expected-dependency-obsolete-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson, IdShouldBeProvidedInBaseData);
    }
    
    [TestMethod]
    public async Task TrackDependency()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackDependency("SQL", "GetOrders", "SELECT * FROM Orders", DateTimeOffset.Now,
                TimeSpan.FromMilliseconds(123), true);

        await VerifyTrackMethod(ClientConsumer, "dependency/expected-dependency.json", IdShouldBeProvidedInBaseData);
    }

    [TestMethod]
    public async Task TrackDependencyWithSeveralMethodArguments
        ()
    {
        var dependencyTypeName = "SQL";
        var target = "my-database";
        var dependencyName = "SELECT";
        var data = "SELECT * FROM Table";
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-5);
        var duration = TimeSpan.FromSeconds(5);
        var resultCode = "2";
        var success = true;

        await VerifyTrackMethod(
            c => c.TrackDependency(dependencyTypeName, target, dependencyName, data, startTime, duration, resultCode,
                success),
            "dependency/expected-dependency-with-several-method-arguments.json"
        );
    }

    [TestMethod]
    public async Task TrackDependencyWithNullTelemetryObject()
    {
        DependencyTelemetry noDependency = null;

        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackDependency(noDependency);

        await VerifyTrackMethod(ClientConsumer, "dependency/expected-dependency-with-null-telemetry-object.json",
            IdShouldBeProvidedInBaseData);
    }

    [TestMethod]
    public async Task TrackDependencyWithTelemetryDependencyObject()
    {
        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackDependency(ADependencyTelemetry());

        await VerifyTrackMethod(ClientConsumer, "dependency/expected-dependency-with-telemetry-dependency-object.json",
            IdShouldBeProvidedInBaseData,
            NodeNameShouldBeEqualTo(NodeName));
    }
    
      private static DependencyTelemetry ADependencyTelemetry()
    {
        var dependencyTelemetry = new DependencyTelemetry
        {
            Id = "dep-12345",
            Name = "SQL GetOrders",
            Target = "sqlserver01.database.windows.net",
            Data = "SELECT * FROM Orders",
            Type = "SQL",
            Duration = TimeSpan.FromMilliseconds(150),
            Timestamp = DateTimeOffset.UtcNow,
            Success = true,
            ResultCode = "0",
            CommandName = "ExecuteReader",
            Properties = { { "Key1", "Value1" }, { "Key2", "Value2" } }
        };

        dependencyTelemetry.Metrics["Metric1"] = 42.0;
        dependencyTelemetry.Metrics["Metric2"] = 3.14;

        dependencyTelemetry.Context.User.Id = "user-001";
        dependencyTelemetry.Context.User.AccountId = "account-abc";
        dependencyTelemetry.Context.Session.Id = "session-xyz";
        dependencyTelemetry.Context.Device.Id = "device-555";
        dependencyTelemetry.Context.Device.Model = "Surface Pro";
        dependencyTelemetry.Context.Device.Type = "PC";
        dependencyTelemetry.Context.Device.OemName = "Microsoft";
        dependencyTelemetry.Context.Device.OperatingSystem = "Windows 10";
        dependencyTelemetry.Context.Device.Language = "fr-FR";
        dependencyTelemetry.Context.Location.Ip = "192.0.2.1";
        dependencyTelemetry.Context.Cloud.RoleName = "api";
        dependencyTelemetry.Context.Cloud.RoleInstance = "instance-01";
        dependencyTelemetry.Context.Component.Version = "2.1.0";
        dependencyTelemetry.Context.Operation.Id = "op-req-01";
        dependencyTelemetry.Context.Operation.Name = "SampleOperation";
        dependencyTelemetry.Context.Operation.ParentId = "op-root-01";
        dependencyTelemetry.Context.Operation.CorrelationVector = "cvector-001";
        dependencyTelemetry.Context.Internal.SdkVersion = "dotnet:2.18.0";
        dependencyTelemetry.Context.Internal.NodeName = NodeName;
        dependencyTelemetry.Context.InstrumentationKey = "00000000-0000-0000-0000-000000000000";

        return dependencyTelemetry;
    }
}