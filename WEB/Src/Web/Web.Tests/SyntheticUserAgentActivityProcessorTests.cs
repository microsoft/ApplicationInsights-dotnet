namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Xunit;

    public class SyntheticUserAgentActivityProcessorTests : ActivityProcessorTestBase
    {
        private const string BotSubstrings = "search|spider|crawl|Bot|Monitor|AlwaysOn";

        [Fact]
        public void OnEnd_DoesNotThrowWhenActivityIsNull()
        {
            // Arrange
            var processor = new SyntheticUserAgentActivityProcessor();

            // Act & Assert
            processor.OnEnd(null);
        }

        [Fact]
        public void OnEnd_DoesNotThrowWhenHttpContextIsNull()
        {
            // Arrange
            SetupTracerProvider(new SyntheticUserAgentActivityProcessor());

            // Act & Assert - Should not throw
            using var activity = StartTestActivity();
        }

        [Fact]
        public void OnEnd_DoesNotSetSyntheticSourceIfAlreadySet()
        {
            // Arrange
            var processor = new SyntheticUserAgentActivityProcessor();
            processor.Filters = BotSubstrings;
            
            var headers = new Dictionary<string, string> { { "User-Agent", "YandexBot" } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(processor);

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
        public void OnEnd_DoesNotSetSyntheticSourceIfNoMatch()
        {
            // Arrange
            var processor = new SyntheticUserAgentActivityProcessor();
            processor.Filters = BotSubstrings;
            
            var headers = new Dictionary<string, string> { { "User-Agent", "RegularBrowser/1.0" } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(processor);

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var syntheticSource = activity.GetTagItem("ai.operation.syntheticSource");
            Assert.Null(syntheticSource);
        }

        [Theory]
        [InlineData("YottaaMonitor 123")]
        [InlineData("HttpMonitor 123")]
        [InlineData("YandexBot 123")]
        [InlineData("ThumbShotsBot 123")]
        [InlineData("Catchpoint bot 123")]
        [InlineData("Willow Internet Crawler 123")]
        [InlineData("AlwaysOn 123")]
        [InlineData("bingbot 123")]
        [InlineData("DBot 123")]
        [InlineData("CCBot 123")]
        [InlineData("crawl 123")]
        [InlineData("spider 123")]
        [InlineData("msnbot 123")]
        [InlineData("msrbot 123")]
        [InlineData("crawler 123")]
        [InlineData("openbot 123")]
        [InlineData("gigabot 123")]
        [InlineData("furlbot 123")]
        [InlineData("polybot 123")]
        [InlineData("EtaoSpider 123")]
        [InlineData("PaperLiBot 123")]
        [InlineData("SputnikBot 123")]
        [InlineData("baiduspider 123")]
        [InlineData("YisouSpider 123")]
        [InlineData("ICC-Crawler 123")]
        [InlineData("converacrawler 123")]
        [InlineData("Sogou Pic Spider 123")]
        [InlineData("Innovazion Crawler 123")]
        public void OnEnd_SetsSyntheticSourceForBots(string userAgent)
        {
            // Arrange
            var processor = new SyntheticUserAgentActivityProcessor();
            processor.Filters = BotSubstrings;
            
            var headers = new Dictionary<string, string> { { "User-Agent", userAgent } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(processor);

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var syntheticSource = activity.GetTagItem("ai.operation.syntheticSource");
            Assert.NotNull(syntheticSource);
            Assert.Equal("Bot", syntheticSource.ToString());
        }

        [Fact]
        public void OnEnd_SetsSyntheticSourceForMultipleItemsFromSameContext()
        {
            // Arrange
            var processor = new SyntheticUserAgentActivityProcessor();
            processor.Filters = BotSubstrings;

            var headers = new Dictionary<string, string> { { "User-Agent", "YottaaMonitor 123" } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(processor);

            Activity activity1;
            using (activity1 = StartTestActivity())
            {
                Assert.NotNull(activity1);
            }

            Activity activity2;
            using (activity2 = StartTestActivity())
            {
                Assert.NotNull(activity2);
            }

            // Assert
            var syntheticSource1 = activity1.GetTagItem("ai.operation.syntheticSource");
            var syntheticSource2 = activity2.GetTagItem("ai.operation.syntheticSource");
            
            Assert.Equal("Bot", syntheticSource1.ToString());
            Assert.Equal("Bot", syntheticSource2.ToString());
        }

        [Fact]
        public void OnEnd_MatchesFiltersCaseInsensitively()
        {
            // Arrange
            var processor = new SyntheticUserAgentActivityProcessor();
            processor.Filters = BotSubstrings;
            
            var headers = new Dictionary<string, string> { { "User-Agent", "YANDEXBOT lowercase test" } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(processor);

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var syntheticSource = activity.GetTagItem("ai.operation.syntheticSource");
            Assert.NotNull(syntheticSource);
            Assert.Equal("Bot", syntheticSource.ToString());
        }

        [Fact]
        public void OnEnd_SupportsCustomFilters()
        {
            // Arrange
            var processor = new SyntheticUserAgentActivityProcessor();
            processor.Filters = "CustomBot|AnotherBot";
            
            var headers = new Dictionary<string, string> { { "User-Agent", "MyCustomBot/1.0" } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(processor);

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            }

            // Assert
            var syntheticSource = activity.GetTagItem("ai.operation.syntheticSource");
            Assert.NotNull(syntheticSource);
            Assert.Equal("Bot", syntheticSource.ToString());
        }
    }
}
