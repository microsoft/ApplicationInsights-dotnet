namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using System;
    using System.Globalization;
    using Helpers;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;

    using Xunit;    

    public class AzureWebAppRoleEnvironmentTelemetryInitializerTest
    {
        [Fact]
        public void AzureWebAppRoleEnvironmentTelemetryInitializerSetsRoleName()
        {
            var telemetryItem = new EventTelemetry();

            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "TestRoleName");
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "TestRoleInstanceName");

            var context = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);
            var initializer = new AzureWebAppRoleEnvironmentTelemetryInitializer(context);
            initializer.Initialize(telemetryItem);

            Assert.Equal("TestRoleName", telemetryItem.Context.Cloud.RoleName);
            Assert.Equal("TestRoleInstanceName", telemetryItem.Context.Cloud.RoleInstance);
            Assert.Equal("TestRoleInstanceName", telemetryItem.Context.GetInternalContext().NodeName);

            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", null);
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
        }

        [Fact]
        public void AzureWebAppRoleEnvironmentTelemetryInitializerDoesNotOverrideRoleName()
        {
            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "TestRoleName");
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "TestRoleInstanceName");

            var telemetryItem = new EventTelemetry();
            telemetryItem.Context.Cloud.RoleName = "Test";

            var context = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);
            var initializer = new AzureWebAppRoleEnvironmentTelemetryInitializer(context);
            initializer.Initialize(telemetryItem);

            Assert.Equal("Test", telemetryItem.Context.Cloud.RoleName);
            Assert.Equal("TestRoleInstanceName", telemetryItem.Context.Cloud.RoleInstance);
            Assert.Equal("TestRoleInstanceName", telemetryItem.Context.GetInternalContext().NodeName);

            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", null);
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
        }

        [Fact]
        public void AzureWebAppRoleEnvironmentTelemetryInitializerDoesNotOverrideRoleInstance()
        {
            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "TestRoleName");
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "TestRoleInstanceName");

            var telemetryItem = new EventTelemetry();
            telemetryItem.Context.Cloud.RoleInstance = "Test";

            var context = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);
            var initializer = new AzureWebAppRoleEnvironmentTelemetryInitializer(context);
            initializer.Initialize(telemetryItem);

            Assert.Equal("TestRoleName", telemetryItem.Context.Cloud.RoleName);
            Assert.Equal("Test", telemetryItem.Context.Cloud.RoleInstance);
            Assert.Equal("TestRoleInstanceName", telemetryItem.Context.GetInternalContext().NodeName);

            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", null);
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
        }

        [Fact]
        public void AzureWebAppRoleEnvironmentTelemetryInitializerDoesNotOverrideNodeName()
        {
            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "TestRoleName");
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "TestRoleInstanceName");

            var telemetryItem = new EventTelemetry();
            telemetryItem.Context.GetInternalContext().NodeName = "Test";

            var context = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);
            var initializer = new AzureWebAppRoleEnvironmentTelemetryInitializer(context);
            initializer.Initialize(telemetryItem);

            Assert.Equal("TestRoleName", telemetryItem.Context.Cloud.RoleName);
            Assert.Equal("TestRoleInstanceName", telemetryItem.Context.Cloud.RoleInstance);
            Assert.Equal("Test", telemetryItem.Context.GetInternalContext().NodeName);

            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", null);
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
        }

        [Fact]
        public void AzureWebAppRoleEnvironmentTelemetryInitializerEmptyVariable()
        {
            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", null);
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);

            var telemetryItem = new EventTelemetry();

            var context = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);
            var initializer = new AzureWebAppRoleEnvironmentTelemetryInitializer(context);
            initializer.Initialize(telemetryItem);

            Assert.Null(telemetryItem.Context.Cloud.RoleName);
            Assert.Null(telemetryItem.Context.Cloud.RoleInstance);
            Assert.Null(telemetryItem.Context.GetInternalContext().NodeName);
        }
    }
}
