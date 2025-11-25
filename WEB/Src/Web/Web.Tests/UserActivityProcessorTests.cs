namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Xunit;

    public class UserActivityProcessorTests : ActivityProcessorTestBase
    {
        [Fact]
        public void OnEnd_DoesNotThrowWhenActivityIsNull()
        {
            // Arrange
            var processor = new UserActivityProcessor();

            // Act & Assert
            processor.OnEnd(null);
        }

        [Fact]
        public void OnEnd_DoesNotThrowWhenHttpContextIsNull()
        {
            // Arrange
            SetupTracerProvider(new UserActivityProcessor());

            // Act & Assert - Should not throw
            using var activity = StartTestActivity();
        }

        [Fact]
        public void OnEnd_SetsUserIdFromCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_user", "user123|" + DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture)) { HttpOnly = true, Secure = true });
            SetupTracerProvider(new UserActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var userId = activity.GetTagItem("ai.user.id");
            Assert.NotNull(userId);
            Assert.Equal("user123", userId.ToString());
        }

        [Fact]
        public void OnEnd_DoesNotSetUserIdWhenCookieIsNull()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            SetupTracerProvider(new UserActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var userId = activity.GetTagItem("ai.user.id");
            Assert.Null(userId);
        }

        [Fact]
        public void OnEnd_DoesNotSetUserIdWhenCookieIsEmpty()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_user", string.Empty) { HttpOnly = true, Secure = true });
            SetupTracerProvider(new UserActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var userId = activity.GetTagItem("ai.user.id");
            Assert.Null(userId);
        }

        [Fact]
        public void OnEnd_DoesNotOverrideExistingUserId()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_user", "user123|" + DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture)) { HttpOnly = true, Secure = true });
            SetupTracerProvider(new UserActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
                activity.SetTag("ai.user.id", "existingUser");
            } // Activity ends, processor should not override

            // Assert
            var userId = activity.GetTagItem("ai.user.id");
            Assert.Equal("existingUser", userId.ToString());
        }

        [Fact]
        public void OnEnd_HandlesIncompleteCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_user", "user123") { HttpOnly = true, Secure = true });
            SetupTracerProvider(new UserActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var userId = activity.GetTagItem("ai.user.id");
            Assert.Null(userId); // Should not set if cookie format is incomplete
        }

        [Fact]
        public void OnEnd_ReadsCookieWithMoreThanTwoParts()
        {
            // Arrange
            var time = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_user", "user123|" + time + "|extraPart") { HttpOnly = true, Secure = true });
            SetupTracerProvider(new UserActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var userId = activity.GetTagItem("ai.user.id");
            Assert.NotNull(userId);
            Assert.Equal("user123", userId.ToString());
        }

        [Fact]
        public void OnEnd_DoesNotSetUserIdWhenTimestampIsMalformed()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_user", "user123|NotATimestamp") { HttpOnly = true, Secure = true });
            SetupTracerProvider(new UserActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var userId = activity.GetTagItem("ai.user.id");
            Assert.Null(userId); // Should not set if timestamp is invalid
        }

        [Fact]
        public void OnEnd_ValidatesTimestampFormat()
        {
            // Arrange
            var validTimestamp = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.AddRequestCookie(new HttpCookie("ai_user", $"user123|{validTimestamp}") { HttpOnly = true, Secure = true });
            SetupTracerProvider(new UserActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var userId = activity.GetTagItem("ai.user.id");
            Assert.NotNull(userId);
            Assert.Equal("user123", userId.ToString());
        }
    }
}
