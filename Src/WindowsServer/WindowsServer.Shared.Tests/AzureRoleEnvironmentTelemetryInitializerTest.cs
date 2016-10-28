namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System.Globalization;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    using Microsoft.ApplicationInsights.WindowsServer.Azure;
    using Microsoft.ApplicationInsights.WindowsServer.Azure.Emulation;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;
    
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class AzureRoleEnvironmentTelemetryInitializerTest
    {
        [TestMethod]
        public void AzureRoleEnvironmentTelemetryInitializerSetsTelemetryContextPropertiesWhenRoleEnvironmentIsAvailable()
        {
            var telemetryItem = new EventTelemetry();
            var initializer = new AzureRoleEnvironmentTelemetryInitializer();
            AzureRoleEnvironmentContextReader.BaseDirectory = ServiceRuntimeHelper.TestWithServiceRuntimePath;
            AzureRoleEnvironmentContextReader.Instance = null;

            ServiceRuntimeHelper.IsAvailable = true;
            initializer.Initialize(telemetryItem);

            string expectedRoleInstanceName = string.Format(
                                    CultureInfo.InvariantCulture,
                                    TestRoleInstance.IdFormat,
                                    ServiceRuntimeHelper.RoleName,
                                    ServiceRuntimeHelper.RoleInstanceOrdinal);

            Assert.Equal(ServiceRuntimeHelper.RoleName, telemetryItem.Context.Cloud.RoleName);
            Assert.Equal(expectedRoleInstanceName, telemetryItem.Context.Cloud.RoleInstance);
            Assert.Equal(expectedRoleInstanceName, telemetryItem.Context.GetInternalContext().NodeName);
        }

        [TestMethod]
        public void AzureRoleEnvironmentTelemetryInitializerDoesNotOverrideRoleName()
        {
            var telemetryItem = new EventTelemetry();
            AzureRoleEnvironmentTelemetryInitializer initializer = new AzureRoleEnvironmentTelemetryInitializer();
            AzureRoleEnvironmentContextReader.BaseDirectory = ServiceRuntimeHelper.TestWithServiceRuntimePath;
            AzureRoleEnvironmentContextReader.Instance = null;
            ServiceRuntimeHelper.IsAvailable = true;

            telemetryItem.Context.Cloud.RoleName = "Test";
            initializer.Initialize(telemetryItem);

            Assert.Equal("Test", telemetryItem.Context.Cloud.RoleName);
        }

        [TestMethod]
        public void AzureRoleEnvironmentTelemetryInitializerDoesNotOverrideRoleInstance()
        {
            var telemetryItem = new EventTelemetry();
            AzureRoleEnvironmentTelemetryInitializer initializer = new AzureRoleEnvironmentTelemetryInitializer();
            AzureRoleEnvironmentContextReader.BaseDirectory = ServiceRuntimeHelper.TestWithServiceRuntimePath;
            AzureRoleEnvironmentContextReader.Instance = null;
            ServiceRuntimeHelper.IsAvailable = true;

            telemetryItem.Context.Cloud.RoleInstance = "Test";
            initializer.Initialize(telemetryItem);

            Assert.Equal("Test", telemetryItem.Context.Cloud.RoleInstance);
        }

        [TestMethod]
        public void AzureRoleEnvironmentTelemetryInitializerDoesNotOverrideNodeName()
        {
            var telemetryItem = new EventTelemetry();
            AzureRoleEnvironmentTelemetryInitializer initializer = new AzureRoleEnvironmentTelemetryInitializer();
            AzureRoleEnvironmentContextReader.BaseDirectory = ServiceRuntimeHelper.TestWithServiceRuntimePath;
            AzureRoleEnvironmentContextReader.Instance = null;
            ServiceRuntimeHelper.IsAvailable = true;

            telemetryItem.Context.GetInternalContext().NodeName = "Test";
            initializer.Initialize(telemetryItem);

            Assert.Equal("Test", telemetryItem.Context.GetInternalContext().NodeName);
        }

        [TestMethod]
        public void AzureRoleEnvironmentTelemetryInitializerSetsTelemetryContextPropertiesWhenRoleEnvironmentIsNotAvailable()
        {
            var telemetryItem = new EventTelemetry();
            AzureRoleEnvironmentTelemetryInitializer initializer = new AzureRoleEnvironmentTelemetryInitializer();
            AzureRoleEnvironmentContextReader.BaseDirectory = ServiceRuntimeHelper.TestWithServiceRuntimePath;
            AzureRoleEnvironmentContextReader.Instance = null;

            ServiceRuntimeHelper.IsAvailable = false;
            initializer.Initialize(telemetryItem);
            ServiceRuntimeHelper.IsAvailable = true;

            Assert.Null(telemetryItem.Context.Cloud.RoleName);
            Assert.Null(telemetryItem.Context.Cloud.RoleInstance);
            Assert.Null(telemetryItem.Context.GetInternalContext().NodeName);
        }
    }
}
