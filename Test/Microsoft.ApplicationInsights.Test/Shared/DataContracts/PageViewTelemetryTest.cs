namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
#if !NETCOREAPP1_1
    using CompareLogic = KellermanSoftware.CompareNetObjects.CompareLogic;
#endif


    [TestClass]
    public class PageViewTelemetryTest
    {
        [TestMethod]
        public void PageViewImplementsITelemetryContractConsistentlyWithOtherTelemetryTypes()
        {
            new ITelemetryTest<PageViewTelemetry, AI.PageViewData>().Run();
        }

        [TestMethod]
        public void PageViewTelemetryIsPublic()
        {
            Assert.IsTrue(typeof(PageViewTelemetry).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void PageViewTelemetryReturnsNonNullContext()
        {
            PageViewTelemetry item = new PageViewTelemetry();
            Assert.IsNotNull(item.Context);
        }

        [TestMethod]
        public void PageViewTelemetrySuppliesConstructorThatTakesNameParameter()
        {
            string expectedPageName = "My page view";
            var instance = new PageViewTelemetry(expectedPageName);
            Assert.AreEqual(expectedPageName, instance.Name);
        }

        [TestMethod]
        public void PageViewTelemetryReturnsDefaultDurationAsTimespanZero()
        {
            PageViewTelemetry item = new PageViewTelemetry();
            Assert.AreEqual(TimeSpan.Zero, item.Duration);
        }

        [TestMethod]
        public void PageViewTelemetrySerializesToJsonCorrectly()
        {
            var expected = new PageViewTelemetry("My Page");
            expected.Url = new Uri("http://temp.org/page1");
            expected.Duration = TimeSpan.FromSeconds(123);
            expected.Metrics.Add("Metric1", 30);
            expected.Properties.Add("Property1", "Value1");

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<PageViewTelemetry, AI.PageViewData>(expected);

            // NOTE: It's correct that we use the v1 name here, and therefore we test against it.
            Assert.AreEqual(item.name, AI.ItemType.PageView);

            Assert.AreEqual(typeof(AI.PageViewData).Name, item.data.baseType);
            Assert.AreEqual(2, item.data.baseData.ver);
            Assert.AreEqual(expected.Name, item.data.baseData.name);
            Assert.AreEqual(expected.Duration, TimeSpan.Parse(item.data.baseData.duration));
            Assert.AreEqual(expected.Url.ToString(), item.data.baseData.url);

            Assert.AreEqual(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFields()
        {
            PageViewTelemetry telemetry = new PageViewTelemetry();
            telemetry.Name = new string('Z', Property.MaxNameLength + 1);
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'X', new string('X', Property.MaxValueLength + 1));
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'Y', new string('X', Property.MaxValueLength + 1));
            telemetry.Metrics.Add(new string('Y', Property.MaxDictionaryNameLength) + 'X', 42.0);
            telemetry.Metrics.Add(new string('Y', Property.MaxDictionaryNameLength) + 'Y', 42.0);
            telemetry.Url = new Uri("http://foo.com/" + new string('Y', Property.MaxUrlLength + 1));

            ((ITelemetry)telemetry).Sanitize();

            Assert.AreEqual(new string('Z', Property.MaxNameLength), telemetry.Name);

            Assert.AreEqual(2, telemetry.Properties.Count);
            string[] keys = telemetry.Properties.Keys.OrderBy(s => s).ToArray();
            string[] values = telemetry.Properties.Values.OrderBy(s => s).ToArray();
            Assert.AreEqual(new string('X', Property.MaxDictionaryNameLength), keys[1]);
            Assert.AreEqual(new string('X', Property.MaxValueLength), values[1]);
            Assert.AreEqual(new string('X', Property.MaxDictionaryNameLength - 3) + "1", keys[0]);
            Assert.AreEqual(new string('X', Property.MaxValueLength), values[0]);

            Assert.AreEqual(2, telemetry.Metrics.Count);
            keys = telemetry.Metrics.Keys.OrderBy(s => s).ToArray();
            Assert.AreEqual(new string('Y', Property.MaxDictionaryNameLength), keys[1]);
            Assert.AreEqual(new string('Y', Property.MaxDictionaryNameLength - 3) + "1", keys[0]);

            Assert.AreEqual(new Uri("http://foo.com/" + new string('Y', Property.MaxUrlLength - 15)), telemetry.Url);
        }

        [TestMethod]
        public void SanitizePopulatesNameWithErrorBecauseItIsRequiredByEndpoint()
        {
            var telemetry = new PageViewTelemetry { Name = null };

            ((ITelemetry)telemetry).Sanitize();

            Assert.AreEqual("n/a", telemetry.Name);
        }

        [TestMethod]
        public void PageViewTelemetryImplementsISupportSamplingContract()
        {
            var telemetry = new PageViewTelemetry();

            Assert.IsNotNull(telemetry as ISupportSampling);
        }

        [TestMethod]
        public void PageViewTelemetryHasCorrectValueOfSamplingPercentageAfterSerialization()
        {
            var telemetry = new PageViewTelemetry("my page view");
            ((ISupportSampling)telemetry).SamplingPercentage = 10;

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<PageViewTelemetry, AI.PageViewData>(telemetry);

            Assert.AreEqual(10, item.sampleRate);
        }

#if !NETCOREAPP1_1
        [TestMethod]
        public void PageViewTelemetryDeepCloneCopiesAllProperties()
        {
            var pageView = new PageViewTelemetry("My Page");
            pageView.Url = new Uri("http://temp.org/page1");
            pageView.Duration = TimeSpan.FromSeconds(123);
            pageView.Metrics.Add("Metric1", 30);
            pageView.Properties.Add("Property1", "Value1");

            PageViewTelemetry other = (PageViewTelemetry)pageView.DeepClone();

            CompareLogic deepComparator = new CompareLogic();
            var result = deepComparator.Compare(pageView, other);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }
#endif
    }
}
