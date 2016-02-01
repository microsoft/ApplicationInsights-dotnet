namespace Microsoft.ApplicationInsights.Web
{
    using System.Collections.Generic;
    using System.Web;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SyntheticUserAgentTelemetryInitializerTest
    {
        // These should match what's in ApplicationInsights.config.install.xdt.
        private SyntheticUserAgentFilter BotRegex = new SyntheticUserAgentFilter
        {
            Pattern = "(YottaaMonitor|BrowserMob|HttpMonitor|YandexBot|BingPreview|PagePeeker|ThumbShotsBot|WebThumb|URL2PNG|ZooShot|GomezA|Catchpoint bot|Willow Internet Crawler|Google SketchUp|Read%20Later|KTXN|Pingdom|AlwaysOn)",
        };

        private SyntheticUserAgentFilter YahooBotRegex = new SyntheticUserAgentFilter
        {
            Pattern = "Slurp",
            SourceName = "Yahoo Bot"
        };

        private SyntheticUserAgentFilter SpiderRegex = new SyntheticUserAgentFilter
        {
            Pattern = @"(bot|zao|borg|Bot|oegp|silk|Xenu|zeal|^NING|crawl|Crawl|htdig|lycos|slurp|teoma|voila|yahoo|Sogou|CiBra|Nutch|^Java/|^JNLP/|Daumoa|Genieo|ichiro|larbin|pompos|Scrapy|snappy|speedy|spider|Spider|vortex|favicon|indexer|Riddler|scooter|scraper|scrubby|WhatWeb|WinHTTP|^voyager|archiver|Icarus6j|mogimogi|Netvibes|altavista|charlotte|findlinks|Retreiver|TLSProber|WordPress|wsr\-agent|Squrl Java|A6\-Indexer|netresearch|searchsight|http%20client|Python-urllib|dataparksearch|Screaming Frog|AppEngine-Google|YahooCacheSystem|semanticdiscovery|facebookexternalhit|Google.*/\+/web/snippet|Google-HTTP-Java-Client)",
            SourceName = "Spider"
        };

        [TestMethod]
        public void SyntheticSourceIsNotSetIfUserProvidedValue()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            metricTelemetry.Context.Operation.SyntheticSource = "SOURCE";
            var source = new TestableSyntheticUserAgentTelemetryInitializer(new Dictionary<string, string>
                {
                    { "User-Agent", "YandexBot" }
                });

            source.Filters.Add(new SyntheticUserAgentFilter
            {
                Pattern = "(YottaaMonitor|BrowserMob|HttpMonitor|YandexBot|BingPreview|PagePeeker|ThumbShotsBot|WebThumb|URL2PNG|ZooShot|GomezA|Catchpoint bot|Willow Internet Crawler|Google SketchUp|Read%20Later)",
            });

            source.Initialize(metricTelemetry);

            Assert.AreEqual("SOURCE", metricTelemetry.Context.Operation.SyntheticSource);
        }

        [TestMethod]
        public void SyntheticSourceIsSetToRegexMatchIfNoReadableSourceProvided()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new TestableSyntheticUserAgentTelemetryInitializer(new Dictionary<string, string>
                {
                    { "User-Agent", "YandexBot 102983" }
                });

            source.Filters.Add(new SyntheticUserAgentFilter
            {
                Pattern = "(YottaaMonitor|BrowserMob|HttpMonitor|YandexBot|BingPreview|PagePeeker|ThumbShotsBot|WebThumb|URL2PNG|ZooShot|GomezA|Catchpoint bot|Willow Internet Crawler|Google SketchUp|Read%20Later)",
            });

            source.Initialize(metricTelemetry);

            Assert.AreEqual("YandexBot", metricTelemetry.Context.Operation.SyntheticSource);
        }

        [TestMethod]
        public void SyntheticSourceIsSetToProvidedSourceName()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new TestableSyntheticUserAgentTelemetryInitializer(new Dictionary<string, string>
                {
                    { "User-Agent", "YandexBot 102983" }
                });

            source.Filters.Add(new SyntheticUserAgentFilter
            {
                Pattern = "(YottaaMonitor|BrowserMob|HttpMonitor|YandexBot|BingPreview|PagePeeker|ThumbShotsBot|WebThumb|URL2PNG|ZooShot|GomezA|Catchpoint bot|Willow Internet Crawler|Google SketchUp|Read%20Later)",
                SourceName = "Robot"
            });

            source.Initialize(metricTelemetry);

            Assert.AreEqual("Robot", metricTelemetry.Context.Operation.SyntheticSource);
        }

        [TestMethod]
        public void SyntheticSourceIsNotSetIfNoMatch()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new TestableSyntheticUserAgentTelemetryInitializer(new Dictionary<string, string>
                {
                    { "User-Agent", "Yan23232dexBot" }
                });

            source.Filters.Add(new SyntheticUserAgentFilter
            {
                Pattern = "(YottaaMonitor|BrowserMob|HttpMonitor|YandexBot|BingPreview|PagePeeker|ThumbShotsBot|WebThumb|URL2PNG|ZooShot|GomezA|Catchpoint bot|Willow Internet Crawler|Google SketchUp|Read%20Later)",
                SourceName = "Robot"
            });

            source.Initialize(metricTelemetry);

            Assert.IsNull(metricTelemetry.Context.Operation.SyntheticSource);
        }

        [TestMethod]
        public void SyntheticSourceIsSetForAllBots()
        {
            AssertSyntheticSourceIsSet("YottaaMonitor", "YottaaMonitor 123");
            AssertSyntheticSourceIsSet("BrowserMob", "BrowserMob 123");
            AssertSyntheticSourceIsSet("HttpMonitor", "HttpMonitor 123");
            AssertSyntheticSourceIsSet("YandexBot", "YandexBot 123");
            AssertSyntheticSourceIsSet("BingPreview", "BingPreview 123");
            AssertSyntheticSourceIsSet("PagePeeker", "PagePeeker 123");
            AssertSyntheticSourceIsSet("ThumbShotsBot", "ThumbShotsBot 123");
            AssertSyntheticSourceIsSet("WebThumb", "WebThumb 123");
            AssertSyntheticSourceIsSet("URL2PNG", "URL2PNG 123");
            AssertSyntheticSourceIsSet("ZooShot", "ZooShot 123");
            AssertSyntheticSourceIsSet("GomezA", "GomezA 123");
            AssertSyntheticSourceIsSet("Catchpoint bot", "Catchpoint bot 123");
            AssertSyntheticSourceIsSet("Willow Internet Crawler", "Willow Internet Crawler 123");
            AssertSyntheticSourceIsSet("Google SketchUp", "Google SketchUp 123");
            AssertSyntheticSourceIsSet("Read%20Later", "Read%20Later 123");
            AssertSyntheticSourceIsSet("KTXN", "KTXN 123");
            AssertSyntheticSourceIsSet("AlwaysOn", "AlwaysOn 123");
        }

        [TestMethod]
        public void SyntheticSourceIsSetForYahooBot()
        {
            AssertSyntheticSourceIsSet("Yahoo Bot", "Slurp 123");
        }

        [TestMethod]
        public void SyntheticSourceIsSetForAllSpiders()
        {
            AssertSyntheticSourceIsSet("Spider", "bingbot 123");
            AssertSyntheticSourceIsSet("Spider", "zao 123");
            AssertSyntheticSourceIsSet("Spider", "borg 123");
            AssertSyntheticSourceIsSet("Spider", "DBot 123");
            AssertSyntheticSourceIsSet("Spider", "oegp 123");
            AssertSyntheticSourceIsSet("Spider", "silk 123");
            AssertSyntheticSourceIsSet("Spider", "Xenu 123");
            AssertSyntheticSourceIsSet("Spider", "zeal 123");
            AssertSyntheticSourceIsSet("Spider", "NING 123");
            AssertSyntheticSourceIsSet("Spider", "CCBot 123");
            AssertSyntheticSourceIsSet("Spider", "crawl 123");
            AssertSyntheticSourceIsSet("Spider", "htdig 123");
            AssertSyntheticSourceIsSet("Spider", "lycos 123");
            AssertSyntheticSourceIsSet("Spider", "slurp 123");
            AssertSyntheticSourceIsSet("Spider", "teoma 123");
            AssertSyntheticSourceIsSet("Spider", "voila 123");
            AssertSyntheticSourceIsSet("Spider", "yahoo 123");
            AssertSyntheticSourceIsSet("Spider", "Sogou 123");
            AssertSyntheticSourceIsSet("Spider", "CiBra 123");
            AssertSyntheticSourceIsSet("Spider", "Java/ 123");
            AssertSyntheticSourceIsSet("Spider", "JNLP/ 123");
            AssertSyntheticSourceIsSet("Spider", "Daumoa 123");
            AssertSyntheticSourceIsSet("Spider", "Genieo 123");
            AssertSyntheticSourceIsSet("Spider", "ichiro 123");
            AssertSyntheticSourceIsSet("Spider", "larbin 123");
            AssertSyntheticSourceIsSet("Spider", "pompos 123");
            AssertSyntheticSourceIsSet("Spider", "Scrapy 123");
            AssertSyntheticSourceIsSet("Spider", "snappy 123");
            AssertSyntheticSourceIsSet("Spider", "speedy 123");
            AssertSyntheticSourceIsSet("Spider", "spider 123");
            AssertSyntheticSourceIsSet("Spider", "msnbot 123");
            AssertSyntheticSourceIsSet("Spider", "msrbot 123");
            AssertSyntheticSourceIsSet("Spider", "123 vortex 123");
            AssertSyntheticSourceIsSet("Spider", "vortex 123");
            AssertSyntheticSourceIsSet("Spider", "crawler 123");
            AssertSyntheticSourceIsSet("Spider", "favicon 123");
            AssertSyntheticSourceIsSet("Spider", "indexer 123");
            AssertSyntheticSourceIsSet("Spider", "Riddler 123");
            AssertSyntheticSourceIsSet("Spider", "scooter 123");
            AssertSyntheticSourceIsSet("Spider", "scraper 123");
            AssertSyntheticSourceIsSet("Spider", "scrubby 123");
            AssertSyntheticSourceIsSet("Spider", "WhatWeb 123");
            AssertSyntheticSourceIsSet("Spider", "WinHTTP 123");
            AssertSyntheticSourceIsSet("Spider", "bingbot 123");
            AssertSyntheticSourceIsSet("Spider", "openbot 123");
            AssertSyntheticSourceIsSet("Spider", "gigabot 123");
            AssertSyntheticSourceIsSet("Spider", "furlbot 123");
            AssertSyntheticSourceIsSet("Spider", "polybot 123");
            AssertSyntheticSourceIsSet("Spider", "voyager 123");
            AssertSyntheticSourceIsSet("Spider", "archiver 123");
            AssertSyntheticSourceIsSet("Spider", "Icarus6j 123");
            AssertSyntheticSourceIsSet("Spider", "Netvibes 123");
            AssertSyntheticSourceIsSet("Spider", "mogimogi 123");
            AssertSyntheticSourceIsSet("Spider", "blitzbot 123");
            AssertSyntheticSourceIsSet("Spider", "altavista 123");
            AssertSyntheticSourceIsSet("Spider", "charlotte 123");
            AssertSyntheticSourceIsSet("Spider", "findlinks 123");
            AssertSyntheticSourceIsSet("Spider", "Retreiver 123");
            AssertSyntheticSourceIsSet("Spider", "TLSProber 123");
            AssertSyntheticSourceIsSet("Spider", "WordPress 123");
            AssertSyntheticSourceIsSet("Spider", "SeznamBot 123");
            AssertSyntheticSourceIsSet("Spider", "ProoXiBot 123");
            AssertSyntheticSourceIsSet("Spider", "wsr-agent 123");
            AssertSyntheticSourceIsSet("Spider", "Squrl Java 123");
            AssertSyntheticSourceIsSet("Spider", "EtaoSpider 123");
            AssertSyntheticSourceIsSet("Spider", "PaperLiBot 123");
            AssertSyntheticSourceIsSet("Spider", "SputnikBot 123");
            AssertSyntheticSourceIsSet("Spider", "A6-Indexer 123");
            AssertSyntheticSourceIsSet("Spider", "netresearch 123");
            AssertSyntheticSourceIsSet("Spider", "searchsight 123");
            AssertSyntheticSourceIsSet("Spider", "baiduspider 123");
            AssertSyntheticSourceIsSet("Spider", "YisouSpider 123");
            AssertSyntheticSourceIsSet("Spider", "ICC-Crawler 123");
            AssertSyntheticSourceIsSet("Spider", "http%20client 123");
            AssertSyntheticSourceIsSet("Spider", "Python-urllib 123");
            AssertSyntheticSourceIsSet("Spider", "dataparksearch 123");
            AssertSyntheticSourceIsSet("Spider", "converacrawler 123");
            AssertSyntheticSourceIsSet("Spider", "Screaming Frog 123");
            AssertSyntheticSourceIsSet("Spider", "AppEngine-Google 123");
            AssertSyntheticSourceIsSet("Spider", "YahooCacheSystem 123");
            AssertSyntheticSourceIsSet("Spider", "fast-webcrawler 123");
            AssertSyntheticSourceIsSet("Spider", "Sogou Pic Spider 123");
            AssertSyntheticSourceIsSet("Spider", "semanticdiscovery 123");
            AssertSyntheticSourceIsSet("Spider", "Innovazion Crawler 123");
            AssertSyntheticSourceIsSet("Spider", "facebookexternalhit 123");
            AssertSyntheticSourceIsSet("Spider", "Google3434/+/web/snippet 123");
            AssertSyntheticSourceIsSet("Spider", "Google-HTTP-Java-Client 123");
        }

        private void AssertSyntheticSourceIsSet(string expectedSource, string userAgent)
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new TestableSyntheticUserAgentTelemetryInitializer(new Dictionary<string, string>
                {
                    { "User-Agent", userAgent }
                });

            source.Filters.Add(BotRegex);
            source.Filters.Add(YahooBotRegex);
            source.Filters.Add(SpiderRegex);

            source.Initialize(metricTelemetry);

            Assert.AreEqual(expectedSource, metricTelemetry.Context.Operation.SyntheticSource);
        }

        private class TestableSyntheticUserAgentTelemetryInitializer : SyntheticUserAgentTelemetryInitializer
        {
            private readonly HttpContext fakeContext;

            public TestableSyntheticUserAgentTelemetryInitializer(IDictionary<string, string> headers = null)
            {
                this.fakeContext = HttpModuleHelper.GetFakeHttpContext(headers);
            }

            protected override HttpContext ResolvePlatformContext()
            {
                return this.fakeContext;
            }
        }
    }
}
