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
        
        await VerifyTrackMethod(ClientConsumer, "dependency/expected-dependency-obsolete.json", IdShouldBeProvidedInBaseData);
    }
    
    [TestMethod]
    public async Task TrackSqlDependencyWithSimpleMethod()
    {
        var dependencyTypeName = "SQL";
        var dependencyName = "SELECT";
        var data = "SELECT * FROM Table";
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-5);
        var duration = TimeSpan.FromSeconds(5);
        var success = true;

        await VerifyTrackMethod(
            c => c.TrackDependency(dependencyTypeName, dependencyName, data, startTime, duration, success), "dependency/expected-dependency-simple-method-sql.json"
        );
    }
    
    [TestMethod]
    public async Task TrackHTTPDependencyWithSimpleMethod()
    {
        var dependencyTypeName = "HTTP";
        var dependencyName = "GET /api/orders";
        var data = "https://api.example.com/api/orders";
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-5);
        var duration = TimeSpan.FromSeconds(2);
        var success = true;

        await VerifyTrackMethod(
            c => c.TrackDependency(dependencyTypeName, dependencyName, data, startTime, duration, success), "dependency/expected-dependency-simple-method-http.json"
        );
    }
    
    [TestMethod]
    public async Task TrackSqlMsSqlDependencyWithSeveralMethodArguments
        ()
    {
        var dependencyTypeName = "SQL";
        // Only "mssql" is mapped to the SQL type by the .net exporter today: https://github.com/Azure/azure-sdk-for-net/blob/7bcb4cd862cc692320c8692eba16321df21ea196/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/src/Internals/AzMonListExtensions.cs#L18
        var target = "mssql";
        var dependencyName = "SELECT";
        var data = "SELECT * FROM Table";
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-5);
        var duration= TimeSpan.FromSeconds(5);
        var resultCode = "0";
        var success = true;

        await VerifyTrackMethod(
            c => c.TrackDependency(dependencyTypeName, target, dependencyName, data, startTime, duration, resultCode,
                success), "dependency/expected-dependency-with-several-method-arguments-sql.json"
        );
    }
    
    [TestMethod]
    public async Task TrackSqlOracleDependencyWithSeveralMethodArguments()
    {
        var dependencyTypeName = "SQL";
        var target = "oracle";
        var dependencyName = "SELECT";
        var data = "SELECT * FROM ORDERS";
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-5);
        var duration = TimeSpan.FromSeconds(5);
        var resultCode = "0";
        var success = true;

        await VerifyTrackMethod(
            c => c.TrackDependency(dependencyTypeName, target, dependencyName, data, startTime, duration, resultCode,
                success), "dependency/expected-dependency-with-several-method-arguments-sql-oracle.json"
        );
    }
    
    [TestMethod]
    public async Task TrackHTTPDependencyWithSeveralMethodArguments()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
        {
            telemetryClient.Context.Properties["Key1"] = "Value1";
            telemetryClient.Context.Properties["Key2"] = "Value2";
            telemetryClient.Context.GlobalProperties["global-Key1"] = "global-Value1";
            telemetryClient.Context.GlobalProperties["global-Key2"] = "global-Value2";
            
            var dependencyTypeName = "HTTP";
            var target = "api.example.com.target";
            var dependencyName = "GET /api/orders";
            var data = "https://api.example.com/api/orders";
            var startTime = DateTimeOffset.UtcNow.AddSeconds(-5);
            var duration = TimeSpan.FromSeconds(2);
            var resultCode = "200";
            var success = false;

            telemetryClient.TrackDependency(dependencyTypeName, target, dependencyName, data, startTime, duration,
                resultCode,
                success);
        }
        
        await VerifyTrackMethod(ClientConsumer, "dependency/expected-dependency-with-several-method-arguments-http.json");
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