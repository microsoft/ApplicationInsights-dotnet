namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    

    [TestClass]
    public class TelemetryContextTest
    {
        [TestMethod]
        public void TelemetryContextIsPublicAndMeantToBeUsedByCustomers()
        {
            Assert.IsTrue(typeof(TelemetryContext).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void TelemetryContextIsSealedToSupportCompilationAsWinmd()
        {
            Assert.IsTrue(typeof(TelemetryContext).GetTypeInfo().IsSealed);
        }

        [TestMethod]
        public void InstrumentationKeyIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.IsNotNull(context.InstrumentationKey);
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
            Assert.IsNotNull(context.Component);
        }

        [TestMethod]
        public void DeviceIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.IsNotNull(context.Device);
        }

        [TestMethod]
        public void SessionIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.IsNotNull(context.Session);
        }

        [TestMethod]
        public void UserIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.IsNotNull(context.User);
        }

        [TestMethod]
        public void OperationIsNotNullByDefaultToPreventNullReferenceExceptionsInUserCode()
        {
            var context = new TelemetryContext();
            Assert.IsNotNull(context.Operation);
        }

        [TestMethod]
        public void LocationIsNotNullByDefaultToPreventNullReferenceExceptionInUserCode()
        {
            TelemetryContext context = new TelemetryContext();
            Assert.IsNotNull(context.Location);
        }

        [TestMethod]
        public void InternalIsNotNullByDefaultToPreventNullReferenceExceptionInUserCode()
        {
            TelemetryContext context = new TelemetryContext();
            Assert.IsNotNull(context.Internal);
        }

        [TestMethod]
        public void InitializeSetsTelemetryInstrumentationKeyFromSource()
        {
            var source = new TelemetryContext { InstrumentationKey = "TestValue" };
            var target = new TelemetryContext();

            target.Initialize(source, source.InstrumentationKey);

            Assert.AreEqual("TestValue", target.InstrumentationKey);
        }

        [TestMethod]
        public void InitializeSetsTelemetryInstrumentationKeyFromArgument()
        {
            var source = new TelemetryContext { InstrumentationKey = "TestValue" };
            var target = new TelemetryContext();

            target.Initialize(source, "OtherTestValue");

            Assert.AreEqual("OtherTestValue", target.InstrumentationKey);
        }

        [TestMethod]
        public void InitializeDoesNotOverrideTelemetryInstrumentationKey()
        {
            var source = new TelemetryContext { InstrumentationKey = "SourceValue" };
            var target = new TelemetryContext { InstrumentationKey = "TargetValue" };

            target.Initialize(source, source.InstrumentationKey);

            Assert.AreEqual("TargetValue", target.InstrumentationKey);
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
                Telemetry.WriteTelemetryContext(new JsonWriter(stringWriter), source);
                return stringWriter.ToString();
            }
        }
    }
}
