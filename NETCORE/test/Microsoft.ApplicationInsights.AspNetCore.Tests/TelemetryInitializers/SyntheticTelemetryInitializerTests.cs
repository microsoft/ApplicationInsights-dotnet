namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Xunit;

    public class SyntheticTelemetryInitializerTests
    {
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new SyntheticTelemetryInitializer(null, null); });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };

            var initializer = new SyntheticTelemetryInitializer(ac, null);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };

            var initializer = new SyntheticTelemetryInitializer(ac, null);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeSetSyntheticTrafficToDesiredValue()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = null;
            contextAccessor.HttpContext.Request.Headers.Add("SyntheticTest-Location", new string[] { "location" });
            contextAccessor.HttpContext.Request.Headers.Add("SyntheticTest-RunId", new string[] { "runId" });

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Application Insights Availability Monitoring", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotHaveSyntheticTrafficSpecified()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = "some value";

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("some value", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeRequestDoesNotHaveRunIdHeaderButHasLocationHeader()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = null;
            contextAccessor.HttpContext.Request.Headers.Add("SyntheticTest-Location", new string[] { "location" });
            contextAccessor.HttpContext.Request.Headers.Remove("SyntheticTest-RunId");
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "Non Synthetic User Agent");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeRequestDoesHaveRunIdHeaderButNoLocationHeader()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = null;
            contextAccessor.HttpContext.Request.Headers.Remove("SyntheticTest-Location");
            contextAccessor.HttpContext.Request.Headers.Add("SyntheticTest-RunId", new string[] { "runId" });
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "Non Synthetic User Agent");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeRequestValidateLocationAndUserAreSetCorrectly()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            var desiredLocation = "location_runId";

            requestTelemetry.Context.Operation.SyntheticSource = null;
            contextAccessor.HttpContext.Request.Headers.Add("SyntheticTest-Location", new string[] { "location" });
            contextAccessor.HttpContext.Request.Headers.Add("SyntheticTest-RunId", new string[] { "runId" });

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Equal(desiredLocation, requestTelemetry.Context.User.Id);
            Assert.Equal("runId", requestTelemetry.Context.Session.Id);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToDefaultDesiredValueIfUserAgentIsNotSet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToDefaultDesiredValueIfUserAgentIsEmpty()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "");
            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToDefaultDesiredValueIfSyntheticSourceIsNotAlreadySet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "Test-Bot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetSyntheticSourceToDefaultDesiredValueIfSyntheticSourceIsNotAlreadySet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "User");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToDefaultDesiredValueIfSyntheticSourceIsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Operation.SyntheticSource = null;
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "Test-Bot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetSyntheticSourceToDefaultDesiredValueIfSyntheticSourceIsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Operation.SyntheticSource = null;
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "User");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToDefaultDesiredValueIfSyntheticSourceIsEmpty()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Operation.SyntheticSource = "";
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "Test-Bot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetSyntheticSourceToDefaultDesiredValueIfSyntheticSourceIsEmpty()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Operation.SyntheticSource = "";
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "User");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetDefaultSyntheticSourceIfSyntheticSourceIsAlreadySet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = "some value";
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "Test-Bot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("some value", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToConfigurationDefaultDesiredValueIfSyntheticSourceIsNotSet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "Test-Bot");
            var mockConfiguration = new Mock<IConfiguration>();

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetSyntheticSourceToConfigurationDefaultDesiredValueIfSyntheticSourceIsNotSet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "User");
            var mockConfiguration = new Mock<IConfiguration>();

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToConfigurationDefaultDesiredValueIfSyntheticSourceIsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = null;
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "Test-Bot");
            var mockConfiguration = new Mock<IConfiguration>();

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetSyntheticSourceToConfigurationDefaultDesiredValueIfSyntheticSourceIsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = null;
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "User");
            var mockConfiguration = new Mock<IConfiguration>();

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetConfigurationDefaultSyntheticSourceIfSyntheticSourceIsAlreadySet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = "some value";
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "Test-Bot");
            var mockConfiguration = new Mock<IConfiguration>();

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("some value", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToConfigurationDesiredValueIfSyntheticSourceIsNotSet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "TestBot");
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["ApplicationInsights:SyntheticUserAgentFilters"])
                .Returns("TestBot|AnotherTestBot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetSyntheticSourceToConfigurationDesiredValueIfSyntheticSourceIsNotSet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "User");
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["ApplicationInsights:SyntheticUserAgentFilters"])
                .Returns("TestBot|AnotherTestBot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToConfigurationDesiredValueIfSyntheticSourceIsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = null;
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "TestBot");
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["ApplicationInsights:SyntheticUserAgentFilters"])
                .Returns("TestBot|AnotherTestBot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetSyntheticSourceToConfigurationDesiredValueIfSyntheticSourceIsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = null;
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "User");
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["ApplicationInsights:SyntheticUserAgentFilters"])
                .Returns("TestBot|AnotherTestBot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetConfigurationSyntheticSourceIfSyntheticSourceIsAlreadySet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = "some value";
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "TestBot");
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["ApplicationInsights:SyntheticUserAgentFilters"])
                .Returns("TestBot|AnotherTestBot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("some value", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToCustomDesiredValueIfSyntheticSourceIsNotSet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "CustomBot");
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["ApplicationInsights:SyntheticUserAgentFilters"])
                .Returns("TestBot|AnotherTestBot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);
            initializer.SyntheticUserAgentFilters = "CustomBot|AnotherCustomBot";

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetSyntheticSourceToCustomDesiredValueIfSyntheticSourceIsNotSet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "User");
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["ApplicationInsights:SyntheticUserAgentFilters"])
                .Returns("TestBot|AnotherTestBot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);
            initializer.SyntheticUserAgentFilters = "CustomBot|AnotherCustomBot";

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceToCustomDesiredValueIfSyntheticSourceIsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = null;
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "CustomBot");
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["ApplicationInsights:SyntheticUserAgentFilters"])
                .Returns("TestBot|AnotherTestBot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);
            initializer.SyntheticUserAgentFilters = "CustomBot|AnotherCustomBot";

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetSyntheticSourceToCustomDesiredValueIfSyntheticSourceIsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = null;
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "User");
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["ApplicationInsights:SyntheticUserAgentFilters"])
                .Returns("TestBot|AnotherTestBot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);
            initializer.SyntheticUserAgentFilters = "CustomBot|AnotherCustomBot";

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeDoesNotSetCustomSyntheticSourceIfSyntheticSourceIsAlreadySet()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            requestTelemetry.Context.Operation.SyntheticSource = "some value";
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "TestBot");
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["ApplicationInsights:SyntheticUserAgentFilters"])
                .Returns("TestBot|AnotherTestBot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, mockConfiguration.Object);
            initializer.SyntheticUserAgentFilters = "CustomBot|AnotherCustomBot";

            initializer.Initialize(requestTelemetry);

            Assert.Equal("some value", requestTelemetry.Context.Operation.SyntheticSource);
        }

        [Fact]
        public void InitializeSetSyntheticSourceOnSeveralTelemetryItemsToDesiredValueIfSyntheticSourceIsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers.Add("User-Agent", "Test-Bot");

            var initializer = new SyntheticTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);
            TraceTelemetry traceTelemetry = new TraceTelemetry();
            initializer.Initialize(traceTelemetry);

            Assert.Equal("Bot", requestTelemetry.Context.Operation.SyntheticSource);
            Assert.Equal("Bot", traceTelemetry.Context.Operation.SyntheticSource);
        }
    }
}
