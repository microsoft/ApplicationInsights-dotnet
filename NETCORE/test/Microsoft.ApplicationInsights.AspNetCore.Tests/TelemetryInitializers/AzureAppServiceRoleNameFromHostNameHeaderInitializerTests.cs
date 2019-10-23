namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using System;
    using System.Net;

    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;

    public class AzureAppServiceRoleNameFromHostNameHeaderInitializerTests : IDisposable
    {
        public AzureAppServiceRoleNameFromHostNameHeaderInitializerTests()
        {
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "SomeName");
        }

        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.ThrowsAny<ArgumentNullException>(() => { var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(null);  });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor { HttpContext = null };
            
            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeFallsbackToEnvIfHttpContextIsUnavailable()
        {
            try
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "RoleNameEnv");
                var ac = new HttpContextAccessor { HttpContext = null };

                var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(ac);
                var req = new RequestTelemetry();
                initializer.Initialize(req);

                Assert.Equal("RoleNameEnv", req.Context.Cloud.RoleName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
            }
        }

        [Fact]
        public void InitializeFallsbackToEnvIfHttpContextIsUnavailableWithAzureWebsitesHostnameending()
        {
            try
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "RoleNameEnv.azurewebsites.net");
                var ac = new HttpContextAccessor { HttpContext = null };

                var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(ac);
                var req = new RequestTelemetry();
                initializer.Initialize(req);

                Assert.Equal("RoleNameEnv", req.Context.Cloud.RoleName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
            }
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestServicesAreUnavailable()
        {
            var ac = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            
            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeFallsbackToEnvIfRequestServicesAreUnavailable()
        {
            try
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "RoleNameEnv");
                var ac = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

                var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(ac);
                var req = new RequestTelemetry();
                initializer.Initialize(req);

                Assert.Equal("RoleNameEnv", req.Context.Cloud.RoleName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
            }
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestIsUnavailable()
        {
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessorWithoutRequest(new HttpContextStub(), new RequestTelemetry());
            
            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);

            initializer.Initialize(new EventTelemetry());
        }

        [Fact]
        public void InitializeFallsbackToEnvIfRequestIsUnavailable()
        {
            try
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "RoleNameEnv");
                var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessorWithoutRequest(new HttpContextStub(), new RequestTelemetry());

                var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);
                var req = new RequestTelemetry();
                initializer.Initialize(req);

                Assert.Equal("RoleNameEnv", req.Context.Cloud.RoleName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
            }
        }

        [Fact]
        public void InitializeDoesNotThrowIfHeaderCollectionIsUnavailable()
        {
            var httpContext = new HttpContextStub();
            httpContext.OnRequestGetter = () => new HttpRequestStub(httpContext);

            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessorWithoutRequest(httpContext, new RequestTelemetry());

            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);

            initializer.Initialize(new EventTelemetry());
        }

        [Fact]
        public void InitializeFallsbackToEnvIfHeaderCollectionIsUnavailable()
        {
            try
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "RoleNameEnv");
                var httpContext = new HttpContextStub();
                httpContext.OnRequestGetter = () => new HttpRequestStub(httpContext);

                var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessorWithoutRequest(httpContext, new RequestTelemetry());

                var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);
                var req = new RequestTelemetry();
                initializer.Initialize(req);

                Assert.Equal("RoleNameEnv", req.Context.Cloud.RoleName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
            }
        }

        [Fact]
        public void InitializeDoesNotThrowIfHostNameHeaderIsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);

            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);

            initializer.Initialize(requestTelemetry);
        }

        [Fact]
        public void InitializeFallsbackToEnvIfHostNameHeaderIsNull()
        {
            try
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "RoleNameEnv");
                var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor();

                var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);
                var req = new RequestTelemetry();
                initializer.Initialize(req);

                Assert.Equal("RoleNameEnv", req.Context.Cloud.RoleName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
            }
        }

        [Fact]
        public void InitializeFallsbackToEnvIfHostNameIsEmptyInHeader()
        {
            try
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "RoleNameEnv");
                var requestTelemetry = new RequestTelemetry();
                var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);

                contextAccessor.HttpContext.Request.Headers.Add("WAS-DEFAULT-HOSTNAME", new string[] { string.Empty });

                var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);

                initializer.Initialize(requestTelemetry);

                Assert.Equal("RoleNameEnv", requestTelemetry.Context.Cloud.RoleName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
            }
        }

        [Fact]
        public void InitializeSetsRoleNameFromHostNameHeader()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);

            contextAccessor.HttpContext.Request.Headers.Add("WAS-DEFAULT-HOSTNAME", new string[] { "MyAppServiceProd" });

            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("MyAppServiceProd", requestTelemetry.Context.Cloud.RoleName);
        }

        [Fact]
        public void InitializeSetsRoleNameFromRequestTelemetryIfPresent()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Cloud.RoleName = "RoleNameOnRequest";
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);

            contextAccessor.HttpContext.Request.Headers.Add("WAS-DEFAULT-HOSTNAME", new string[] { "RoleNameOnHeader" });

            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);

            var evt = new EventTelemetry();
            initializer.Initialize(evt);

            Assert.Equal("RoleNameOnRequest", evt.Context.Cloud.RoleName);
        }

        [Fact]
        public void InitializeSavesRoleNameIntoRequestFromHostNameHeader()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);

            contextAccessor.HttpContext.Request.Headers.Add("WAS-DEFAULT-HOSTNAME", new string[] { "MyAppServiceProd" });

            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);

            var evt = new EventTelemetry();
            initializer.Initialize(evt);

            Assert.Equal("MyAppServiceProd", evt.Context.Cloud.RoleName);
            Assert.Equal("MyAppServiceProd", requestTelemetry.Context.Cloud.RoleName);
        }

        [Fact]
        public void InitializeSetsRoleNameFromHostNameWithAzureWebsites()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);

            contextAccessor.HttpContext.Request.Headers.Add("WAS-DEFAULT-HOSTNAME", new string[] { "appserviceslottest-ppe.azurewebsites.net" });

            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("appserviceslottest-ppe", requestTelemetry.Context.Cloud.RoleName);
        }

        [Fact]
        public void InitializeSetsRoleNameFromHostNameWithAzureWebsitesCustom()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);

            contextAccessor.HttpContext.Request.Headers.Add("WAS-DEFAULT-HOSTNAME", new string[] { "appserviceslottest-ppe.azurewebsites.us" });

            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);
            initializer.WebAppSuffix = ".azurewebsites.us";

            initializer.Initialize(requestTelemetry);

            Assert.Equal("appserviceslottest-ppe", requestTelemetry.Context.Cloud.RoleName);
        }

        [Fact]
        public void InitializeSetsRoleNameFromEnvWithAzureWebsitesCustom()
        {
            try
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "appserviceslottest-ppe.azurewebsites.us");
                var requestTelemetry = new RequestTelemetry();
                var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
                var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor, ".azurewebsites.us");

                initializer.Initialize(requestTelemetry);

                Assert.Equal("appserviceslottest-ppe", requestTelemetry.Context.Cloud.RoleName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
            }
        }

        [Fact]
        public void InitializeDoesNotOverrideRoleName()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Cloud.RoleName = "ExistingRoleName";
            
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("WAS-DEFAULT-HOSTNAME", new string[] { "MyAppServiceProd" });

            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(contextAccessor);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("ExistingRoleName", requestTelemetry.Context.Cloud.RoleName);
        }

        [Fact]
        public void InitializewithNoEnvToHostNameHeader()
        {
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
            var ac = new HttpContextAccessor { HttpContext = null };

            var initializer = new AzureAppServiceRoleNameFromHostNameHeaderInitializer(ac);

            var requestTelemetry = new RequestTelemetry();
            initializer.Initialize(requestTelemetry);
            Assert.Null(requestTelemetry.Context.Cloud.RoleName);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
        }
    }
}