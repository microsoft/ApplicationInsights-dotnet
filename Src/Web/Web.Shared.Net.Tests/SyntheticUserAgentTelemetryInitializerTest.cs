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
