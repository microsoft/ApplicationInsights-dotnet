namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http.Core;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.Routing;
    using Microsoft.AspNet.Routing;
    using System;
    using Xunit;

    public class OperationNameTelemetryInitializerTests
    {
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new OperationNameTelemetryInitializer(null); });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };

            var initializer = new OperationNameTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };

            var initializer = new OperationNameTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideOperationNameProvidedInline()
        {
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor);

            var telemetry = new EventTelemetry();
            telemetry.Context.Operation.Name = "Name";
            initializer.Initialize(telemetry);

            Assert.Equal("Name", telemetry.Context.Operation.Name);
        }

        [Fact]
        public void InitializeSetsTelemetryOperationNameToMethodAndPath()
        {
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("GET /Test", telemetry.Context.Operation.Name);
        }

        [Fact]
        public void InitializeSetsRequestNameToMethodAndPath()
        {
            var telemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(telemetry, null);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor);

            initializer.Initialize(telemetry);

            Assert.Equal("GET /Test", telemetry.Name);
        }

        [Fact]
        public void InitializeSetsTelemetryOperationNameToControllerFromActionContext()
        {
            var actionContext = new ActionContext();
            actionContext.RouteData = new RouteData();
            actionContext.RouteData.Values.Add("controller", "home");

            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), actionContext);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("GET home", telemetry.Context.Operation.Name);
        }

        [Fact]
        public void InitializeSetsTelemetryOperationNameToControllerAndActionFromActionContext()
        {
            var actionContext = new ActionContext();
            actionContext.RouteData = new RouteData();
            actionContext.RouteData.Values.Add("controller", "account");
            actionContext.RouteData.Values.Add("action", "login");

            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), actionContext);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("GET account/login", telemetry.Context.Operation.Name);
        }

        [Fact]
        public void InitializeSetsTelemetryOperationNameToControllerAndActionAndParameterFromActionContext()
        {
            var actionContext = new ActionContext();
            actionContext.RouteData = new RouteData();
            actionContext.RouteData.Values.Add("controller", "account");
            actionContext.RouteData.Values.Add("action", "login");
            actionContext.RouteData.Values.Add("parameter", "myName");

            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), actionContext);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("GET account/login [parameter]", telemetry.Context.Operation.Name);
        }

        [Fact]
        public void InitializeSortsParameters()
        {
            var actionContext = new ActionContext();
            actionContext.RouteData = new RouteData();
            actionContext.RouteData.Values.Add("controller", "account");
            actionContext.RouteData.Values.Add("action", "login");
            actionContext.RouteData.Values.Add("parameterZ", "myName1");
            actionContext.RouteData.Values.Add("parameterA", "myName2");
            actionContext.RouteData.Values.Add("parameterN", "myName1");

            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), actionContext);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("GET account/login [parameterA/parameterN/parameterZ]", telemetry.Context.Operation.Name);
        }

        [Fact]
        public void InitializeDoesNotIncludeRouteGroupKeyInParametersList()
        {
            var actionContext = new ActionContext();
            actionContext.RouteData = new RouteData();
            actionContext.RouteData.Values.Add("controller", "account");
            actionContext.RouteData.Values.Add("action", "login");
            actionContext.RouteData.Values.Add(AttributeRouting.RouteGroupKey, "RouteGroupKey");
            
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), actionContext);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("GET account/login", telemetry.Context.Operation.Name);
        }        
    }
}