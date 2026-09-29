namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Xunit;

    public class AccountIdActivityProcessorTests : ActivityProcessorTestBase
    {

        [Fact]
        public void OnEnd_DoesNotThrowWhenActivityIsNull()
        {
            // Arrange
            var processor = new AccountIdActivityProcessor();

            // Act & Assert
            processor.OnEnd(null);
        }

        [Fact]
        public void OnEnd_DoesNotThrowWhenHttpContextIsNull()
        {
            // Arrange
            SetupTracerProvider(new AccountIdActivityProcessor());

            // Act & Assert - Should not throw
            using var activity = StartTestActivity();
        }

        [Fact]
        public void OnEnd_SetsAccountIdFromCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("authUser123|account456");
            SetupTracerProvider(new AccountIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var accountId = activity.GetTagItem("microsoft.user.account_id");
            Assert.NotNull(accountId);
            Assert.Equal("account456", accountId.ToString());
        }

        [Fact]
        public void OnEnd_DoesNotSetAccountIdWhenCookieIsNull()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            SetupTracerProvider(new AccountIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var accountId = activity.GetTagItem("microsoft.user.account_id");
            Assert.Null(accountId);
        }

        [Fact]
        public void OnEnd_DoesNotSetAccountIdWhenCookieIsEmpty()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie(string.Empty);
            SetupTracerProvider(new AccountIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var accountId = activity.GetTagItem("microsoft.user.account_id");
            Assert.Null(accountId);
        }

        [Fact]
        public void OnEnd_DoesNotSetAccountIdWhenCookieDoesNotHaveIt()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("authUser123");
            SetupTracerProvider(new AccountIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var accountId = activity.GetTagItem("microsoft.user.account_id");
            Assert.Null(accountId);
        }

        [Fact]
        public void OnEnd_DoesNotSetAccountIdWhenCookieIsMalformed()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("authUser123|");
            SetupTracerProvider(new AccountIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var accountId = activity.GetTagItem("microsoft.user.account_id");
            Assert.Null(accountId);
        }

        [Fact]
        public void OnEnd_HandlesNonAsciiCharactersInCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("authUser123|account456א");
            SetupTracerProvider(new AccountIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var accountId = activity.GetTagItem("microsoft.user.account_id");
            Assert.NotNull(accountId);
            Assert.Equal("account456א", accountId.ToString());
        }

        [Fact]
        public void OnEnd_HandlesSpecialCharactersInCookie()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("authUser123|$#@!!!!");
            SetupTracerProvider(new AccountIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var accountId = activity.GetTagItem("microsoft.user.account_id");
            Assert.NotNull(accountId);
            Assert.Equal("$#@!!!!", accountId.ToString());
        }

        [Fact]
        public void OnEnd_SetsAccountIdWhenAuthenticatedUserIdIsAlreadySet()
        {
            // Arrange - AuthenticatedUserIdActivityProcessor runs first and sets enduser.id from the same cookie
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("authUser123|account456");
            SetupTracerProvider(new AccountIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
                activity.SetTag("enduser.id", "authUser123");
            }

            // Assert
            Assert.Equal("account456", activity.GetTagItem("microsoft.user.account_id")?.ToString());
            Assert.Equal("authUser123", activity.GetTagItem("enduser.id")?.ToString());
            Assert.Null(activity.GetTagItem("enduser.account"));
        }

        [Fact]
        public void OnEnd_DoesNotOverrideExistingAccountId()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.WithAuthCookie("authUser123|account456");
            SetupTracerProvider(new AccountIdActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
                activity.SetTag("microsoft.user.account_id", "existingAccount");
            }

            // Assert
            Assert.Equal("existingAccount", activity.GetTagItem("microsoft.user.account_id")?.ToString());
        }
    }
}
