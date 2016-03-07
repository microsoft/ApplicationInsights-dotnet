namespace Microsoft.ApplicationInsights.Web
{
    using System.Collections.Generic;
    using System.Web;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SyntheticUserAgentTelemetryInitializerTest
    {
        // These should match what's in ApplicationInsights.config.install.xdt.
        private SyntheticUserAgentFilter botRegex = new SyntheticUserAgentFilter
        {
            Pattern = "(YottaaMonitor|BrowserMob|HttpMonitor|YandexBot|BingPreview|PagePeeker|ThumbShotsBot|WebThumb|URL2PNG|ZooShot|GomezA|Catchpoint bot|Willow Internet Crawler|Google SketchUp|Read%20Later|KTXN|Pingdom|AlwaysOn)",
        };

        private SyntheticUserAgentFilter yahooBotRegex = new SyntheticUserAgentFilter
        {
            Pattern = "Slurp",
            SourceName = "Yahoo Bot"
        };

        private SyntheticUserAgentFilter spiderRegex = new SyntheticUserAgentFilter
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
            this.AssertSyntheticSourceIsSet("YottaaMonitor", "YottaaMonitor 123");
            this.AssertSyntheticSourceIsSet("BrowserMob", "BrowserMob 123");
            this.AssertSyntheticSourceIsSet("HttpMonitor", "HttpMonitor 123");
            this.AssertSyntheticSourceIsSet("YandexBot", "YandexBot 123");
            this.AssertSyntheticSourceIsSet("BingPreview", "BingPreview 123");
            this.AssertSyntheticSourceIsSet("PagePeeker", "PagePeeker 123");
            this.AssertSyntheticSourceIsSet("ThumbShotsBot", "ThumbShotsBot 123");
            this.AssertSyntheticSourceIsSet("WebThumb", "WebThumb 123");
            this.AssertSyntheticSourceIsSet("URL2PNG", "URL2PNG 123");
            this.AssertSyntheticSourceIsSet("ZooShot", "ZooShot 123");
            this.AssertSyntheticSourceIsSet("GomezA", "GomezA 123");
            this.AssertSyntheticSourceIsSet("Catchpoint bot", "Catchpoint bot 123");
            this.AssertSyntheticSourceIsSet("Willow Internet Crawler", "Willow Internet Crawler 123");
            this.AssertSyntheticSourceIsSet("Google SketchUp", "Google SketchUp 123");
            this.AssertSyntheticSourceIsSet("Read%20Later", "Read%20Later 123");
            this.AssertSyntheticSourceIsSet("KTXN", "KTXN 123");
            this.AssertSyntheticSourceIsSet("AlwaysOn", "AlwaysOn 123");
        }

        [TestMethod]
        public void SyntheticSourceIsSetForYahooBot()
        {
            this.AssertSyntheticSourceIsSet("Yahoo Bot", "Slurp 123");
        }

        [TestMethod]
        public void SyntheticSourceIsSetForAllSpiders()
        {
            this.AssertSyntheticSourceIsSet("Spider", "bingbot 123");
            this.AssertSyntheticSourceIsSet("Spider", "zao 123");
            this.AssertSyntheticSourceIsSet("Spider", "borg 123");
            this.AssertSyntheticSourceIsSet("Spider", "DBot 123");
            this.AssertSyntheticSourceIsSet("Spider", "oegp 123");
            this.AssertSyntheticSourceIsSet("Spider", "silk 123");
            this.AssertSyntheticSourceIsSet("Spider", "Xenu 123");
            this.AssertSyntheticSourceIsSet("Spider", "zeal 123");
            this.AssertSyntheticSourceIsSet("Spider", "NING 123");
            this.AssertSyntheticSourceIsSet("Spider", "CCBot 123");
            this.AssertSyntheticSourceIsSet("Spider", "crawl 123");
            this.AssertSyntheticSourceIsSet("Spider", "htdig 123");
            this.AssertSyntheticSourceIsSet("Spider", "lycos 123");
            this.AssertSyntheticSourceIsSet("Spider", "slurp 123");
            this.AssertSyntheticSourceIsSet("Spider", "teoma 123");
            this.AssertSyntheticSourceIsSet("Spider", "voila 123");
            this.AssertSyntheticSourceIsSet("Spider", "yahoo 123");
            this.AssertSyntheticSourceIsSet("Spider", "Sogou 123");
            this.AssertSyntheticSourceIsSet("Spider", "CiBra 123");
            this.AssertSyntheticSourceIsSet("Spider", "Java/ 123");
            this.AssertSyntheticSourceIsSet("Spider", "JNLP/ 123");
            this.AssertSyntheticSourceIsSet("Spider", "Daumoa 123");
            this.AssertSyntheticSourceIsSet("Spider", "Genieo 123");
            this.AssertSyntheticSourceIsSet("Spider", "ichiro 123");
            this.AssertSyntheticSourceIsSet("Spider", "larbin 123");
            this.AssertSyntheticSourceIsSet("Spider", "pompos 123");
            this.AssertSyntheticSourceIsSet("Spider", "Scrapy 123");
            this.AssertSyntheticSourceIsSet("Spider", "snappy 123");
            this.AssertSyntheticSourceIsSet("Spider", "speedy 123");
            this.AssertSyntheticSourceIsSet("Spider", "spider 123");
            this.AssertSyntheticSourceIsSet("Spider", "msnbot 123");
            this.AssertSyntheticSourceIsSet("Spider", "msrbot 123");
            this.AssertSyntheticSourceIsSet("Spider", "123 vortex 123");
            this.AssertSyntheticSourceIsSet("Spider", "vortex 123");
            this.AssertSyntheticSourceIsSet("Spider", "crawler 123");
            this.AssertSyntheticSourceIsSet("Spider", "favicon 123");
            this.AssertSyntheticSourceIsSet("Spider", "indexer 123");
            this.AssertSyntheticSourceIsSet("Spider", "Riddler 123");
            this.AssertSyntheticSourceIsSet("Spider", "scooter 123");
            this.AssertSyntheticSourceIsSet("Spider", "scraper 123");
            this.AssertSyntheticSourceIsSet("Spider", "scrubby 123");
            this.AssertSyntheticSourceIsSet("Spider", "WhatWeb 123");
            this.AssertSyntheticSourceIsSet("Spider", "WinHTTP 123");
            this.AssertSyntheticSourceIsSet("Spider", "bingbot 123");
            this.AssertSyntheticSourceIsSet("Spider", "openbot 123");
            this.AssertSyntheticSourceIsSet("Spider", "gigabot 123");
            this.AssertSyntheticSourceIsSet("Spider", "furlbot 123");
            this.AssertSyntheticSourceIsSet("Spider", "polybot 123");
            this.AssertSyntheticSourceIsSet("Spider", "voyager 123");
            this.AssertSyntheticSourceIsSet("Spider", "archiver 123");
            this.AssertSyntheticSourceIsSet("Spider", "Icarus6j 123");
            this.AssertSyntheticSourceIsSet("Spider", "Netvibes 123");
            this.AssertSyntheticSourceIsSet("Spider", "mogimogi 123");
            this.AssertSyntheticSourceIsSet("Spider", "blitzbot 123");
            this.AssertSyntheticSourceIsSet("Spider", "altavista 123");
            this.AssertSyntheticSourceIsSet("Spider", "charlotte 123");
            this.AssertSyntheticSourceIsSet("Spider", "findlinks 123");
            this.AssertSyntheticSourceIsSet("Spider", "Retreiver 123");
            this.AssertSyntheticSourceIsSet("Spider", "TLSProber 123");
            this.AssertSyntheticSourceIsSet("Spider", "WordPress 123");
            this.AssertSyntheticSourceIsSet("Spider", "SeznamBot 123");
            this.AssertSyntheticSourceIsSet("Spider", "ProoXiBot 123");
            this.AssertSyntheticSourceIsSet("Spider", "wsr-agent 123");
            this.AssertSyntheticSourceIsSet("Spider", "Squrl Java 123");
            this.AssertSyntheticSourceIsSet("Spider", "EtaoSpider 123");
            this.AssertSyntheticSourceIsSet("Spider", "PaperLiBot 123");
            this.AssertSyntheticSourceIsSet("Spider", "SputnikBot 123");
            this.AssertSyntheticSourceIsSet("Spider", "A6-Indexer 123");
            this.AssertSyntheticSourceIsSet("Spider", "netresearch 123");
            this.AssertSyntheticSourceIsSet("Spider", "searchsight 123");
            this.AssertSyntheticSourceIsSet("Spider", "baiduspider 123");
            this.AssertSyntheticSourceIsSet("Spider", "YisouSpider 123");
            this.AssertSyntheticSourceIsSet("Spider", "ICC-Crawler 123");
            this.AssertSyntheticSourceIsSet("Spider", "http%20client 123");
            this.AssertSyntheticSourceIsSet("Spider", "Python-urllib 123");
            this.AssertSyntheticSourceIsSet("Spider", "dataparksearch 123");
            this.AssertSyntheticSourceIsSet("Spider", "converacrawler 123");
            this.AssertSyntheticSourceIsSet("Spider", "Screaming Frog 123");
            this.AssertSyntheticSourceIsSet("Spider", "AppEngine-Google 123");
            this.AssertSyntheticSourceIsSet("Spider", "YahooCacheSystem 123");
            this.AssertSyntheticSourceIsSet("Spider", "fast-webcrawler 123");
            this.AssertSyntheticSourceIsSet("Spider", "Sogou Pic Spider 123");
            this.AssertSyntheticSourceIsSet("Spider", "semanticdiscovery 123");
            this.AssertSyntheticSourceIsSet("Spider", "Innovazion Crawler 123");
            this.AssertSyntheticSourceIsSet("Spider", "facebookexternalhit 123");
            this.AssertSyntheticSourceIsSet("Spider", "Google3434/+/web/snippet 123");
            this.AssertSyntheticSourceIsSet("Spider", "Google-HTTP-Java-Client 123");
        }

        private void AssertSyntheticSourceIsSet(string expectedSource, string userAgent)
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new TestableSyntheticUserAgentTelemetryInitializer(new Dictionary<string, string>
                {
                    { "User-Agent", userAgent }
                });

            source.Filters.Add(this.botRegex);
            source.Filters.Add(this.yahooBotRegex);
            source.Filters.Add(this.spiderRegex);

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
