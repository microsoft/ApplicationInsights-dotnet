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
        private string botRegex = "search|spider|crawl|Bot|Monitor|BrowserMob|BingPreview|PagePeeker|WebThumb|URL2PNG|ZooShot|GomezA|Google SketchUp|Read Later|KTXN|KHTE|Keynote|Pingdom|AlwaysOn|zao|borg|oegp|silk|Xenu|zeal|NING|htdig|lycos|slurp|teoma|voila|yahoo|Sogou|CiBra|Nutch|Java|JNLP|Daumoa|Genieo|ichiro|larbin|pompos|Scrapy|snappy|speedy|vortex|favicon|indexer|Riddler|scooter|scraper|scrubby|WhatWeb|WinHTTP|voyager|archiver|Icarus6j|mogimogi|Netvibes|altavista|charlotte|findlinks|Retreiver|TLSProber|WordPress|wsr-agent|http client|Python-urllib|AppEngine-Google|semanticdiscovery|facebookexternalhit|web/snippet|Google-HTTP-Java-Client";          

        [TestMethod]
        public void SyntheticSourceIsNotSetIfUserProvidedValue()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            metricTelemetry.Context.Operation.SyntheticSource = "SOURCE";
            var source = new TestableSyntheticUserAgentTelemetryInitializer(new Dictionary<string, string>
                {
                    { "User-Agent", "YandexBot" }
                });

            source.Filters = this.botRegex;

            source.Initialize(metricTelemetry);

            Assert.AreEqual("SOURCE", metricTelemetry.Context.Operation.SyntheticSource);
        }

        [TestMethod]
        public void SyntheticSourceIsNotSetIfNoMatch()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new TestableSyntheticUserAgentTelemetryInitializer(new Dictionary<string, string>
                {
                    { "User-Agent", "Yan23232dexBooot" }
                });

            source.Filters = this.botRegex;

            source.Initialize(metricTelemetry);

            Assert.IsNull(metricTelemetry.Context.Operation.SyntheticSource);
        }

        [TestMethod]
        public void SyntheticSourceIsNotSetIfContextIsNull()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new SyntheticUserAgentTelemetryInitializer();

            source.Filters = this.botRegex;

            source.Initialize(metricTelemetry);

            Assert.IsNull(metricTelemetry.Context.Operation.SyntheticSource);
        }                                                            

        [TestMethod]
        public void SyntheticSourceIsSetForAllBots()
        {
            this.AssertSyntheticSourceIsSet("YottaaMonitor 123");
            this.AssertSyntheticSourceIsSet("BrowserMob 123");
            this.AssertSyntheticSourceIsSet("HttpMonitor 123");
            this.AssertSyntheticSourceIsSet("YandexBot 123");
            this.AssertSyntheticSourceIsSet("BingPreview 123");
            this.AssertSyntheticSourceIsSet("PagePeeker 123");
            this.AssertSyntheticSourceIsSet("ThumbShotsBot 123");
            this.AssertSyntheticSourceIsSet("WebThumb 123");
            this.AssertSyntheticSourceIsSet("URL2PNG 123");
            this.AssertSyntheticSourceIsSet("ZooShot 123");
            this.AssertSyntheticSourceIsSet("GomezA 123");
            this.AssertSyntheticSourceIsSet("Catchpoint bot 123");
            this.AssertSyntheticSourceIsSet("Willow Internet Crawler 123");
            this.AssertSyntheticSourceIsSet("Google SketchUp 123");
            this.AssertSyntheticSourceIsSet("Read Later 123");
            this.AssertSyntheticSourceIsSet("KTXN 123");
            this.AssertSyntheticSourceIsSet("AlwaysOn 123");
        
            this.AssertSyntheticSourceIsSet("Slurp 123");
        
            this.AssertSyntheticSourceIsSet("bingbot 123");
            this.AssertSyntheticSourceIsSet("zao 123");
            this.AssertSyntheticSourceIsSet("borg 123");
            this.AssertSyntheticSourceIsSet("DBot 123");
            this.AssertSyntheticSourceIsSet("oegp 123");
            this.AssertSyntheticSourceIsSet("silk 123");
            this.AssertSyntheticSourceIsSet("Xenu 123");
            this.AssertSyntheticSourceIsSet("zeal 123");
            this.AssertSyntheticSourceIsSet("NING 123");
            this.AssertSyntheticSourceIsSet("CCBot 123");
            this.AssertSyntheticSourceIsSet("crawl 123");
            this.AssertSyntheticSourceIsSet("htdig 123");
            this.AssertSyntheticSourceIsSet("lycos 123");
            this.AssertSyntheticSourceIsSet("slurp 123");
            this.AssertSyntheticSourceIsSet("teoma 123");
            this.AssertSyntheticSourceIsSet("voila 123");
            this.AssertSyntheticSourceIsSet("yahoo 123");
            this.AssertSyntheticSourceIsSet("Sogou 123");
            this.AssertSyntheticSourceIsSet("CiBra 123");
            this.AssertSyntheticSourceIsSet("Java/ 123");
            this.AssertSyntheticSourceIsSet("JNLP/ 123");
            this.AssertSyntheticSourceIsSet("Daumoa 123");
            this.AssertSyntheticSourceIsSet("Genieo 123");
            this.AssertSyntheticSourceIsSet("ichiro 123");
            this.AssertSyntheticSourceIsSet("larbin 123");
            this.AssertSyntheticSourceIsSet("pompos 123");
            this.AssertSyntheticSourceIsSet("Scrapy 123");
            this.AssertSyntheticSourceIsSet("snappy 123");
            this.AssertSyntheticSourceIsSet("speedy 123");
            this.AssertSyntheticSourceIsSet("spider 123");
            this.AssertSyntheticSourceIsSet("msnbot 123");
            this.AssertSyntheticSourceIsSet("msrbot 123");
            this.AssertSyntheticSourceIsSet("123 vortex 123");
            this.AssertSyntheticSourceIsSet("vortex 123");
            this.AssertSyntheticSourceIsSet("crawler 123");
            this.AssertSyntheticSourceIsSet("favicon 123");
            this.AssertSyntheticSourceIsSet("indexer 123");
            this.AssertSyntheticSourceIsSet("Riddler 123");
            this.AssertSyntheticSourceIsSet("scooter 123");
            this.AssertSyntheticSourceIsSet("scraper 123");
            this.AssertSyntheticSourceIsSet("scrubby 123");
            this.AssertSyntheticSourceIsSet("WhatWeb 123");
            this.AssertSyntheticSourceIsSet("WinHTTP 123");
            this.AssertSyntheticSourceIsSet("bingbot 123");
            this.AssertSyntheticSourceIsSet("openbot 123");
            this.AssertSyntheticSourceIsSet("gigabot 123");
            this.AssertSyntheticSourceIsSet("furlbot 123");
            this.AssertSyntheticSourceIsSet("polybot 123");
            this.AssertSyntheticSourceIsSet("voyager 123");
            this.AssertSyntheticSourceIsSet("archiver 123");
            this.AssertSyntheticSourceIsSet("Icarus6j 123");
            this.AssertSyntheticSourceIsSet("Netvibes 123");
            this.AssertSyntheticSourceIsSet("mogimogi 123");
            this.AssertSyntheticSourceIsSet("blitzbot 123");
            this.AssertSyntheticSourceIsSet("altavista 123");
            this.AssertSyntheticSourceIsSet("charlotte 123");
            this.AssertSyntheticSourceIsSet("findlinks 123");
            this.AssertSyntheticSourceIsSet("Retreiver 123");
            this.AssertSyntheticSourceIsSet("TLSProber 123");
            this.AssertSyntheticSourceIsSet("WordPress 123");
            this.AssertSyntheticSourceIsSet("SeznamBot 123");
            this.AssertSyntheticSourceIsSet("ProoXiBot 123");
            this.AssertSyntheticSourceIsSet("wsr-agent 123");
            this.AssertSyntheticSourceIsSet("Squrl Java 123");
            this.AssertSyntheticSourceIsSet("EtaoSpider 123");
            this.AssertSyntheticSourceIsSet("PaperLiBot 123");
            this.AssertSyntheticSourceIsSet("SputnikBot 123");
            this.AssertSyntheticSourceIsSet("A6-Indexer 123");
            this.AssertSyntheticSourceIsSet("netresearch 123");
            this.AssertSyntheticSourceIsSet("searchsight 123");
            this.AssertSyntheticSourceIsSet("baiduspider 123");
            this.AssertSyntheticSourceIsSet("YisouSpider 123");
            this.AssertSyntheticSourceIsSet("ICC-Crawler 123");
            this.AssertSyntheticSourceIsSet("http client 123");
            this.AssertSyntheticSourceIsSet("Python-urllib 123");
            this.AssertSyntheticSourceIsSet("dataparksearch 123");
            this.AssertSyntheticSourceIsSet("converacrawler 123");
            this.AssertSyntheticSourceIsSet("AppEngine-Google 123");
            this.AssertSyntheticSourceIsSet("YahooCacheSystem 123");
            this.AssertSyntheticSourceIsSet("fast-webcrawler 123");
            this.AssertSyntheticSourceIsSet("Sogou Pic Spider 123");
            this.AssertSyntheticSourceIsSet("semanticdiscovery 123");
            this.AssertSyntheticSourceIsSet("Innovazion Crawler 123");
            this.AssertSyntheticSourceIsSet("facebookexternalhit 123");
            this.AssertSyntheticSourceIsSet("web/snippet 123");
            this.AssertSyntheticSourceIsSet("Google-HTTP-Java-Client 123");
        }

        private void AssertSyntheticSourceIsSet(string userAgent)
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new TestableSyntheticUserAgentTelemetryInitializer(new Dictionary<string, string>
                {
                    { "User-Agent", userAgent }
                });

            source.Filters = this.botRegex;

            source.Initialize(metricTelemetry);

            Assert.AreEqual("Bot", metricTelemetry.Context.Operation.SyntheticSource, "Incorrect result for " + userAgent);
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
