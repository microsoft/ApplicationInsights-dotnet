namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Xunit;

    public class SessionActivityProcessorTests : ActivityProcessorTestBase
    {
        [Fact]
        public void OnEnd_DoesNotThrowWhenActivityIsNull()
        {
            // Arrange
            var processor = new SessionActivityProcessor();

            // Act & Assert - Should not throw
            processor.OnEnd(null);
        }

        [Fact]
        public void OnEnd_DoesNotThrowWhenHttpContextIsNull()
        {
            // Arrange
            SetupTracerProvider(new SessionActivityProcessor());

            // Act & Assert - Should not throw
            using var activity = StartTestActivity();
        }

        [Fact]
        public void OnEnd_SetsSessionIdFromCookie()
        {
            // Arrange
            string now = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", "session123|" + now + "|" + now) { HttpOnly = true, Secure = true });

            var processor = new SessionActivityProcessor();
            SetupTracerProvider(processor);

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends, processor OnEnd is called

            // Assert
            var sessionId = activity.GetTagItem("microsoft.session.id");
            Assert.NotNull(sessionId);
            Assert.Equal("session123", sessionId.ToString());
        }

        [Fact]
        public void OnEnd_DoesNotSetSessionIdWhenCookieIsNull()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var sessionId = activity.GetTagItem("microsoft.session.id");
            Assert.Null(sessionId);
        }

        [Fact]
        public void OnEnd_DoesNotSetSessionIdWhenCookieIsEmpty()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", string.Empty) { HttpOnly = true, Secure = true });
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var sessionId = activity.GetTagItem("microsoft.session.id");
            Assert.Null(sessionId);
        }

        [Fact]
        public void OnEnd_DoesNotOverrideExistingSessionId()
        {
            // Arrange
            string now = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", "session123|" + now + "|" + now) { HttpOnly = true, Secure = true });
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
                activity.SetTag("microsoft.session.id", "existingSession");
            } // Activity ends, processor should not override

            // Assert
            var sessionId = activity.GetTagItem("microsoft.session.id");
            Assert.Equal("existingSession", sessionId.ToString());
        }

        [Fact]
        public void OnEnd_HandlesIncompleteCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", "session123") { HttpOnly = true, Secure = true });
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var sessionId = activity.GetTagItem("microsoft.session.id");
            Assert.NotNull(sessionId);
            Assert.Equal("session123", sessionId.ToString());
        }

        [Fact]
        public void OnEnd_DoesNotSetUnmappedSessionTags()
        {
            // Arrange - matching acquisition/renewal dates used to produce a session.isFirst tag
            string now = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", $"session123|{now}|{now}") { HttpOnly = true, Secure = true });
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert - only the key mapped by the Azure Monitor exporter is written
            Assert.Equal("session123", activity.GetTagItem("microsoft.session.id")?.ToString());
            Assert.Null(activity.GetTagItem("session.id"));
            Assert.Null(activity.GetTagItem("session.isFirst"));
        }
    }
}
