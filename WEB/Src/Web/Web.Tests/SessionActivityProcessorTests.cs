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
            context.AddRequestCookie(new HttpCookie("ai_session", "session123|" + now + "|" + now) { HttpOnly = true });

            var processor = new SessionActivityProcessor();
            SetupTracerProvider(processor);

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends, processor OnEnd is called

            // Assert
            var sessionId = activity.GetTagItem("session.id");
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
            var sessionId = activity.GetTagItem("session.id");
            Assert.Null(sessionId);
        }

        [Fact]
        public void OnEnd_DoesNotSetSessionIdWhenCookieIsEmpty()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", string.Empty) { HttpOnly = true });
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var sessionId = activity.GetTagItem("session.id");
            Assert.Null(sessionId);
        }

        [Fact]
        public void OnEnd_DoesNotOverrideExistingSessionId()
        {
            // Arrange
            string now = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", "session123|" + now + "|" + now) { HttpOnly = true });
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
                activity.SetTag("session.id", "existingSession");
            } // Activity ends, processor should not override

            // Assert
            var sessionId = activity.GetTagItem("session.id");
            Assert.Equal("existingSession", sessionId.ToString());
        }

        [Fact]
        public void OnEnd_HandlesIncompleteCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", "session123") { HttpOnly = true });
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var sessionId = activity.GetTagItem("session.id");
            Assert.NotNull(sessionId);
            Assert.Equal("session123", sessionId.ToString());
        }

        [Fact]
        public void OnEnd_SetsIsFirstWhenAcquisitionAndRenewalDatesMatch()
        {
            // Arrange
            string now = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", $"session123|{now}|{now}") { HttpOnly = true });
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var sessionId = activity.GetTagItem("session.id");
            var isFirst = activity.GetTagItem("session.isFirst");
            
            Assert.Equal("session123", sessionId.ToString());
            Assert.NotNull(isFirst);
            Assert.True((bool)isFirst);
        }

        [Fact]
        public void OnEnd_DoesNotSetIsFirstWhenAcquisitionAndRenewalDatesDiffer()
        {
            // Arrange
            string acquisitionDate = DateTimeOffset.Now.AddHours(-1).ToString("O", CultureInfo.InvariantCulture);
            string renewalDate = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", $"session123|{acquisitionDate}|{renewalDate}") { HttpOnly = true });
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var sessionId = activity.GetTagItem("session.id");
            var isFirst = activity.GetTagItem("session.isFirst");
            
            Assert.Equal("session123", sessionId.ToString());
            Assert.Null(isFirst); // Should not be set if dates differ
        }

        [Fact]
        public void OnEnd_DoesNotSetIsFirstWhenCookieHasOnlySessionId()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_session", "session123") { HttpOnly = true });
            SetupTracerProvider(new SessionActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var sessionId = activity.GetTagItem("session.id");
            var isFirst = activity.GetTagItem("session.isFirst");
            
            Assert.Equal("session123", sessionId.ToString());
            Assert.Null(isFirst); // Should not be set without dates
        }
    }
}
