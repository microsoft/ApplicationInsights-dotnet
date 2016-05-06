namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.AspNetCore.Routing;
    using Xunit;
    using Microsoft.AspNetCore.Routing.Tree;
    using Microsoft.AspNetCore.Http;
    public class OperationNameTelemetryInitializerTests
    {
        private const string TestListenerName = "TestListener";
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.ThrowsAny<ArgumentNullException>(() => 
            {
                var initializer = new OperationNameTelemetryInitializer(null, new DiagnosticListener(TestListenerName));
            });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };

            var initializer = new OperationNameTelemetryInitializer(ac, new DiagnosticListener(TestListenerName));

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };

            var initializer = new OperationNameTelemetryInitializer(ac, new DiagnosticListener(TestListenerName));

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideOperationNameProvidedInline()
        {
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor, new DiagnosticListener(TestListenerName));

            var telemetry = new EventTelemetry();
            telemetry.Context.Operation.Name = "Name";
            initializer.Initialize(telemetry);

            Assert.Equal("Name", telemetry.Context.Operation.Name);
        }

        [Fact]
        public void InitializeSetsTelemetryOperationNameToMethodAndPath()
        {
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor, new DiagnosticListener(TestListenerName));

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("GET /Test", telemetry.Context.Operation.Name);
        }

        [Fact]
        public void InitializeSetsRequestNameToMethodAndPath()
        {
            var telemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(telemetry, null);

            var initializer = new OperationNameTelemetryInitializer(contextAccessor, new DiagnosticListener(TestListenerName));

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

            var telemetryListener = new DiagnosticListener(TestListenerName);
            var initializer = new OperationNameTelemetryInitializer(contextAccessor, telemetryListener);
            telemetryListener.Write(OperationNameTelemetryInitializer.BeforeActionNotificationName,
                new { httpContext = contextAccessor.HttpContext, routeData = actionContext.RouteData });

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

            var telemetryListener = new DiagnosticListener(TestListenerName);
            var initializer = new OperationNameTelemetryInitializer(contextAccessor, telemetryListener);
            telemetryListener.Write(OperationNameTelemetryInitializer.BeforeActionNotificationName,
                new { httpContext = contextAccessor.HttpContext, routeData = actionContext.RouteData });

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

            var telemetryListener = new DiagnosticListener(TestListenerName);
            var initializer = new OperationNameTelemetryInitializer(contextAccessor, telemetryListener);
            telemetryListener.Write(OperationNameTelemetryInitializer.BeforeActionNotificationName,
                new { httpContext = contextAccessor.HttpContext, routeData = actionContext.RouteData });

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

            var telemetryListener = new DiagnosticListener(TestListenerName);
            var initializer = new OperationNameTelemetryInitializer(contextAccessor, telemetryListener);
            telemetryListener.Write(OperationNameTelemetryInitializer.BeforeActionNotificationName,
                new { httpContext = contextAccessor.HttpContext, routeData = actionContext.RouteData });

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
            actionContext.RouteData.Values.Add(TreeRouter.RouteGroupKey, "RouteGroupKey");
            
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), actionContext);
            var telemetryListener = new DiagnosticListener(TestListenerName);
            var initializer = new OperationNameTelemetryInitializer(contextAccessor, telemetryListener);
            telemetryListener.Write(OperationNameTelemetryInitializer.BeforeActionNotificationName, 
                new { httpContext = contextAccessor.HttpContext, routeData = actionContext.RouteData });

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("GET account/login", telemetry.Context.Operation.Name);
        }        

        [Fact]
        public void InitializeSetsRequestNameToMethodAndPathForPostRequest()
        {
            var telemetry = new RequestTelemetry();

            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(telemetry);
            contextAccessor.HttpContext.Request.Method = "POST";

            var initializer = new OperationNameTelemetryInitializer(contextAccessor, new DiagnosticListener(TestListenerName));

            initializer.Initialize(telemetry);

            Assert.Equal("POST /Test", telemetry.Name);
        }

    }
}