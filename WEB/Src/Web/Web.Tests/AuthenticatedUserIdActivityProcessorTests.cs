namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Xunit;

    public class AuthenticatedUserIdActivityProcessorTests : ActivityProcessorTestBase
    {

        [Fact]
        public void OnEnd_DoesNotThrowWhenActivityIsNull()
        {
            // Arrange
            var processor = new AuthenticatedUserIdActivityProcessor();

            // Act & Assert
            processor.OnEnd(null);
        }

        [Fact]
        public void OnEnd_DoesNotThrowWhenHttpContextIsNull()
        {
            // Arrange
            SetupTracerProvider(new AuthenticatedUserIdActivityProcessor());

            // Act & Assert - Should not throw
            using var activity = StartTestActivity();
        }

        [Fact]
        public void OnEnd_SetsAuthenticatedUserIdFromCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("authUser123|account456");
            SetupTracerProvider(new AuthenticatedUserIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var authUserId = activity.GetTagItem("enduser.id");
            Assert.NotNull(authUserId);
            Assert.Equal("authUser123", authUserId.ToString());
        }

        [Fact]
        public void OnEnd_DoesNotSetAuthUserIdWhenCookieIsNull()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            SetupTracerProvider(new AuthenticatedUserIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var authUserId = activity.GetTagItem("enduser.id");
            Assert.Null(authUserId);
        }

        [Fact]
        public void OnEnd_DoesNotSetAuthUserIdWhenCookieIsEmpty()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie(string.Empty);
            SetupTracerProvider(new AuthenticatedUserIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var authUserId = activity.GetTagItem("enduser.id");
            Assert.Null(authUserId);
        }

        [Fact]
        public void OnEnd_DoesNotOverrideExistingAuthUserId()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("authUser123|account456");
            SetupTracerProvider(new AuthenticatedUserIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
                activity.SetTag("enduser.id", "existingAuthUser");
            }

            // Assert
            var authUserId = activity.GetTagItem("enduser.id");
            Assert.Equal("existingAuthUser", authUserId.ToString());
        }

        [Fact]
        public void OnEnd_HandlesNonAsciiCharactersInCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("authUser123实|account456");
            SetupTracerProvider(new AuthenticatedUserIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var authUserId = activity.GetTagItem("enduser.id");
            Assert.NotNull(authUserId);
            Assert.Equal("authUser123实", authUserId.ToString());
        }

        [Fact]
        public void OnEnd_HandlesSpecialCharactersInCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("$#@!!!!|account123");
            SetupTracerProvider(new AuthenticatedUserIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var authUserId = activity.GetTagItem("enduser.id");
            Assert.NotNull(authUserId);
            Assert.Equal("$#@!!!!", authUserId.ToString());
        }

        [Fact]
        public void OnEnd_HandlesMalformedCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("|");
            SetupTracerProvider(new AuthenticatedUserIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var authUserId = activity.GetTagItem("enduser.id");
            Assert.Null(authUserId);
        }
    }
}
