namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;

    [TestClass]
    public class TelemetryContextTest
    {
        [TestMethod]
        public void TelemetryContextIsPublicAndMeantToBeUsedByCustomers()
        {
            Assert.True(typeof(TelemetryContext).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void TelemetryContextIsSealedToSupportCompilationAsWinmd()
        {
            Assert.True(typeof(TelemetryContext).GetTypeInfo().IsSealed);
        }

        [TestMethod]
        public void ConstructorInitializesTagsWithThreadSafeDictionaryObjects()
        {
            var context = new TelemetryContext();
            Assert.IsType<SnapshottingDictionary<string, string>>(context.Tags);
        }

        [TestMethod]
        public void InstrumentationKeyIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.NotNull(context.InstrumentationKey);
        }

        [TestMethod]
        public void InstrumentationKeySetterThrowsArgumentNullExceptionWhenValueIsNullToPreventNullReferenceExceptionsLater()
        {
            var context = new TelemetryContext();
            Assert.Throws<ArgumentNullException>(() => context.InstrumentationKey = null);
        }
        
        [TestMethod]
        public void ComponentIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.NotNull(context.Component);
        }

        [TestMethod]
        public void DeviceIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.NotNull(context.Device);
        }

        [TestMethod]
        public void SessionIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.NotNull(context.Session);
        }

        [TestMethod]
        public void UserIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.NotNull(context.User);
        }

        [TestMethod]
        public void OperationIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.NotNull(context.Operation);
        }

        [TestMethod]
        public void LocationIsNotNullByDefaultToPreventNullReferenceExceptionInUserCode()
        {
            TelemetryContext context = new TelemetryContext();
            Assert.NotNull(context.Location);
        }

        [TestMethod]
        public void InternalIsNotNullByDefaultToPreventNullReferenceExceptionInUserCode()
        {
            TelemetryContext context = new TelemetryContext();
            Assert.NotNull(context.Internal);
        }

        [TestMethod]
        public void InitializeCopiesTags()
        {
            string tagName = "TestTag";
            string tagValue = "TestValue";
            var source = new TelemetryContext { Tags = { { tagName, tagValue } } };
            var target = new TelemetryContext();

            target.Initialize(source, source.InstrumentationKey);

            Assert.Equal(tagValue, target.Tags[tagName]);
        }

        [TestMethod]
        public void InitializeDoesNotOverwriteTags()
        {
            string tagName = "TestTag";
            var source = new TelemetryContext { Tags = { { tagName, "Source Value" } } };
            var target = new TelemetryContext { Tags = { { tagName, "Target Value" } } };

            target.Initialize(source, source.InstrumentationKey);

            Assert.Equal("Target Value", target.Tags[tagName]);
        }

        [TestMethod]
        public void InitializeSetsTelemetryInstrumentationKeyFromSource()
        {
            var source = new TelemetryContext { InstrumentationKey = "TestValue" };
            var target = new TelemetryContext();

            target.Initialize(source, source.InstrumentationKey);

            Assert.Equal("TestValue", target.InstrumentationKey);
        }

        [TestMethod]
        public void InitializeSetsTelemetryInstrumentationKeyFromArgument()
        {
            var source = new TelemetryContext { InstrumentationKey = "TestValue" };
            var target = new TelemetryContext();

            target.Initialize(source, "OtherTestValue");

            Assert.Equal("OtherTestValue", target.InstrumentationKey);
        }

        [TestMethod]
        public void InitializeDoesNotOverrideTelemetryInstrumentationKey()
        {
            var source = new TelemetryContext { InstrumentationKey = "SourceValue" };
            var target = new TelemetryContext { InstrumentationKey = "TargetValue" };

            target.Initialize(source, source.InstrumentationKey);

            Assert.Equal("TargetValue", target.InstrumentationKey);
        }

        [TestMethod]
        public void SerializeWritesCopiedDeviceContext()
        {
            var context = new TelemetryContext();
            context.Device.Id = "Test Value";
            string json = CopyAndSerialize(context);
            Assert.Contains("\"" + ContextTagKeys.Keys.DeviceId + "\":\"Test Value\"", json, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void SerializeWritesCopiedComponentContext()
        {
            var context = new TelemetryContext();
            context.Component.Version = "Test Value";
            string json = CopyAndSerialize(context);
            Assert.Contains("\"" + ContextTagKeys.Keys.ApplicationVersion + "\":\"Test Value\"", json, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void SerializeWritesCopiedLocationContext()
        {
            var context = new TelemetryContext();
            context.Location.Ip = "1.2.3.4";
            string json = CopyAndSerialize(context);
            Assert.Contains("\"" + ContextTagKeys.Keys.LocationIp + "\":\"1.2.3.4\"", json, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void SerializeWritesCopiedUserContext()
        {
            var context = new TelemetryContext();
            context.User.Id = "Test Value";
            string json = CopyAndSerialize(context);
            Assert.Contains("\"" + ContextTagKeys.Keys.UserId + "\":\"Test Value\"", json, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void SerializeWritesCopiedOperationContext()
        {
            var context = new TelemetryContext();
            context.Operation.Id = "Test Value";
            string json = CopyAndSerialize(context);
            Assert.Contains("\"" + ContextTagKeys.Keys.OperationId + "\":\"Test Value\"", json, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void SerializeWritesCopiedSessionContext()
        {
            var context = new TelemetryContext();
            context.Session.Id = "Test Value";
            string json = CopyAndSerialize(context);
            Assert.Contains("\"" + ContextTagKeys.Keys.SessionId + "\":\"Test Value\"", json, StringComparison.OrdinalIgnoreCase);
        }

        private static string CopyAndSerialize(TelemetryContext source)
        {
            // Create a copy of the source context to verify that Serialize writes property values stored in tags 
            // dictionary even if their context objects (User, Location, etc) haven't been initialized yet.
            var target = new TelemetryContext();
            target.Initialize(source, source.InstrumentationKey);

            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                ((IJsonSerializable)target).Serialize(new JsonWriter(stringWriter));
                return stringWriter.ToString();
            }
        }
    }
}
