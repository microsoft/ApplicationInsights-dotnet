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
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using DataPlatformModel = Microsoft.Developer.Analytics.DataCollection.Model.v2;

    [TestClass]
    public class PageViewTelemetryTest
    {
        [TestMethod]
        public void PageViewImplementsITelemetryContractConsistentlyWithOtherTelemetryTypes()
        {
            new ITelemetryTest<PageViewTelemetry, DataPlatformModel.PageViewData>().Run();
        }

        [TestMethod]
        public void PageViewTelemetryIsPublic()
        {
            Assert.True(typeof(PageViewTelemetry).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void PageViewTelemetryReturnsNonNullContext()
        {
            PageViewTelemetry item = new PageViewTelemetry();
            Assert.NotNull(item.Context);
        }

        [TestMethod]
        public void PageViewTelemetrySuppliesConstructorThatTakesNameParameter()
        {
            string expectedPageName = "My page view";
            var instance = new PageViewTelemetry(expectedPageName);
            Assert.Equal(expectedPageName, instance.Name);
        }

        [TestMethod]
        public void PageViewTelemetryReturnsDefaultDurationAsTimespanZero()
        {
            PageViewTelemetry item = new PageViewTelemetry();
            Assert.Equal(TimeSpan.Zero, item.Duration);
        }

        [TestMethod]
        public void PageViewTelemetrySerializesToJsonCorrectly()
        {
            var expected = new PageViewTelemetry("My Page");
            expected.Url = new Uri("http://temp.org/page1");
            expected.Duration = TimeSpan.FromSeconds(123);
            expected.Metrics.Add("Metric1", 30);
            expected.Properties.Add("Property1", "Value1");

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<PageViewTelemetry, DataPlatformModel.PageViewData>(expected);

            // NOTE: It's correct that we use the v1 name here, and therefore we test against it.
            Assert.Equal(item.Name, Microsoft.Developer.Analytics.DataCollection.Model.v1.ItemType.PageView);

            Assert.Equal(typeof(DataPlatformModel.PageViewData).Name, item.Data.BaseType);
            Assert.Equal(2, item.Data.BaseData.Ver);
            Assert.Equal(expected.Name, item.Data.BaseData.Name);
            Assert.Equal(expected.Duration, item.Data.BaseData.Duration);
            Assert.Equal(expected.Url.ToString(), item.Data.BaseData.Url);
            Assert.Equal(expected.Metrics, item.Data.BaseData.Measurements);

            Assert.Equal(expected.Properties.ToArray(), item.Data.BaseData.Properties.ToArray());
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

            Assert.Equal(new string('Z', Property.MaxNameLength), telemetry.Name);

            Assert.Equal(2, telemetry.Properties.Count);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength), telemetry.Properties.Keys.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength - 3) + "001", telemetry.Properties.Keys.ToArray()[1]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[1]);

            Assert.Equal(2, telemetry.Metrics.Count);
            Assert.Equal(new string('Y', Property.MaxDictionaryNameLength), telemetry.Metrics.Keys.ToArray()[0]);
            Assert.Equal(new string('Y', Property.MaxDictionaryNameLength - 3) + "001", telemetry.Metrics.Keys.ToArray()[1]);

            Assert.Equal(new Uri("http://foo.com/" + new string('Y', Property.MaxUrlLength - 15)), telemetry.Url);
        }

        [TestMethod]
        public void SanitizePopulatesNameWithErrorBecauseItIsRequiredByEndpoint()
        {
            var telemetry = new PageViewTelemetry { Name = null };

            ((ITelemetry)telemetry).Sanitize();

            Assert.Contains("name", telemetry.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("required", telemetry.Name, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void PageViewTelemetryImplementsISupportSamplingContract()
        {
            var telemetry = new PageViewTelemetry();

            Assert.NotNull(telemetry as ISupportSampling);
        }

        [TestMethod]
        public void PageViewTelemetryHasCorrectValueOfSamplingPercentageAfterSerialization()
        {
            var telemetry = new PageViewTelemetry("my page view");
            ((ISupportSampling)telemetry).SamplingPercentage = 10;

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<PageViewTelemetry, DataPlatformModel.PageViewData>(telemetry);

            Assert.Equal(10, item.SampleRate);
        }
    }
}
