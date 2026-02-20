namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Xunit;

    public class WebTestActivityProcessorTests : ActivityProcessorTestBase
    {

        [Fact]
        public void OnEnd_DoesNotThrowWhenActivityIsNull()
        {
            // Arrange
            var processor = new WebTestActivityProcessor();

            // Act & Assert
            processor.OnEnd(null);
        }

        [Fact]
        public void OnEnd_DoesNotThrowWhenHttpContextIsNull()
        {
            // Arrange
            SetupTracerProvider(new WebTestActivityProcessor());

            // Act & Assert - Should not throw
            using var activity = StartTestActivity();
        }

        [Fact]
        public void OnEnd_SetsSyntheticSourceToWellKnownValue()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "SyntheticTest-Location", "LOCATION" },
                { "SyntheticTest-RunId", "ID" }
            };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(new WebTestActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var syntheticSource = activity.GetTagItem("ai.operation.syntheticSource");
            Assert.NotNull(syntheticSource);
            Assert.Equal("Application Insights Availability Monitoring", syntheticSource.ToString());
        }

        [Fact]
        public void OnEnd_DoesNotSetSyntheticSourceIfAlreadySet()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "SyntheticTest-Location", "LOCATION" },
                { "SyntheticTest-RunId", "ID" }
            };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(new WebTestActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
                activity.SetTag("ai.operation.syntheticSource", "ExistingSource");
            }

            // Assert
            var syntheticSource = activity.GetTagItem("ai.operation.syntheticSource");
            Assert.Equal("ExistingSource", syntheticSource.ToString());
        }

        [Fact]
        public void OnEnd_SetsUserIdToLocationPlusRunId()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "SyntheticTest-Location", "LOCATION" },
                { "SyntheticTest-RunId", "ID" }
            };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(new WebTestActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var userId = activity.GetTagItem("ai.user.id");
            Assert.NotNull(userId);
            Assert.Equal("LOCATION_ID", userId.ToString());
        }

        [Fact]
        public void OnEnd_SetsSessionIdToRunId()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "SyntheticTest-Location", "LOCATION" },
                { "SyntheticTest-RunId", "ID" }
            };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(new WebTestActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var sessionId = activity.GetTagItem("session.id");
            Assert.NotNull(sessionId);
            Assert.Equal("ID", sessionId.ToString());
        }

        [Fact]
        public void OnEnd_DoesNotSetPropertiesIfLocationIsNotSet()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "SyntheticTest-RunId", "ID" }
            };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(new WebTestActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            Assert.Null(activity.GetTagItem("ai.user.id"));
            Assert.Null(activity.GetTagItem("session.id"));
            Assert.Null(activity.GetTagItem("ai.operation.syntheticSource"));
        }

        [Fact]
        public void OnEnd_DoesNotSetPropertiesIfRunIdIsNotSet()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "SyntheticTest-Location", "LOCATION" }
            };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(new WebTestActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            Assert.Null(activity.GetTagItem("ai.user.id"));
            Assert.Null(activity.GetTagItem("session.id"));
            Assert.Null(activity.GetTagItem("ai.operation.syntheticSource"));
        }

        [Fact]
        public void OnEnd_HeadersAreCaseInsensitive()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "synthetictest-location", "LOCATION" },
                { "synthetictest-runid", "ID" }
            };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(new WebTestActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var syntheticSource = activity.GetTagItem("ai.operation.syntheticSource");
            Assert.NotNull(syntheticSource);
            Assert.Equal("Application Insights Availability Monitoring", syntheticSource.ToString());
        }

        [Fact]
        public void OnEnd_TruncatesHeaderExceedingMaxLength()
        {
            var longRunId = new string('X', RequestTrackingConstants.RequestHeaderMaxLength + 100);
            var headers = new Dictionary<string, string>
            {
                { "SyntheticTest-Location", "LOC" },
                { "SyntheticTest-RunId", longRunId }
            };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(new WebTestActivityProcessor());

            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            var sessionId = activity.GetTagItem("session.id")?.ToString();
            Assert.NotNull(sessionId);
            Assert.Equal(RequestTrackingConstants.RequestHeaderMaxLength, sessionId.Length);
        }
    }
}
