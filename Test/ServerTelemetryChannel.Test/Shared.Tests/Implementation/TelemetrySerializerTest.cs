namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    public class TelemetrySerializerTest
    {
        [TestClass]
        public class Class : TelemetrySerializerTest
        {
            [TestMethod]
            public void ConstructorThrowsArgumentNullExceptionIfTransmitterIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new TelemetrySerializer(null));
            }
        }

        [TestClass]
        public class EndpointAddress
        {
            [TestMethod]
            public void DefaultValuePointsToModernEndpointInProductionEnvironment()
            {
                var serializer = new TelemetrySerializer(new StubTransmitter());
                Assert.Equal("https://dc.services.visualstudio.com/v2/track", serializer.EndpointAddress.ToString());
            }

            [TestMethod]
            public void SetterThrowsArgumentNullExceptionToPreventUsageErrors()
            {
                var serializer = new TelemetrySerializer(new StubTransmitter());
                Assert.Throws<ArgumentNullException>(() => serializer.EndpointAddress = null);
            }

            [TestMethod]
            public void CanBeChangedByChannelBasedOnConfigurationToRedirectTelemetryToDifferentEnvironment()
            {
                var serializer = new TelemetrySerializer(new StubTransmitter());
                var expectedValue = new Uri("int://environment");
                serializer.EndpointAddress = expectedValue;
                Assert.Equal(expectedValue, serializer.EndpointAddress);
            }
        }

        [TestClass]
        public class SerializeAsync
        {
            [TestMethod]
            public void ThrowsArgumentNullExceptionWhenTelemetryIsNullToPreventUsageErrors()
            {
                var serializer = new TelemetrySerializer(new StubTransmitter());
                Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null));
            }

            [TestMethod]
            public void ThrowsArgumentExceptionWhenTelemetryIsEmptyToPreventUsageErrors()
            {
                var serializer = new TelemetrySerializer(new StubTransmitter());
                Assert.Throws<ArgumentException>(() => serializer.Serialize(new List<ITelemetry>()));
            }

            [TestMethod]
            public void SerializesTelemetryOnSameThreadBecauseItAssumesItIsAlreadyOnThreadPool()
            {
                int serializationThreadId = -1;
                var transmitter = new StubTransmitter();
                transmitter.OnEnqueue = t => 
                {
                    serializationThreadId = Thread.CurrentThread.ManagedThreadId;
                };

                var serializer = new TelemetrySerializer(transmitter);
                serializer.Serialize(new[] { new StubTelemetry() });

                Assert.Equal(serializationThreadId, Thread.CurrentThread.ManagedThreadId);
            }

            [TestMethod]
            public void EnqueuesTransmissionWithExpectedProperties()
            {
                Transmission transmission = null;
                var transmitter = new StubTransmitter();
                transmitter.OnEnqueue = t =>
                {
                    transmission = t;
                };
        
                var serializer = new TelemetrySerializer(transmitter) { EndpointAddress = new Uri("http://expected.uri") };
                serializer.Serialize(new[] { new StubTelemetry() });
        
                Assert.Equal(serializer.EndpointAddress, transmission.EndpointAddress);
                Assert.Equal("application/x-json-stream", transmission.ContentType);
                Assert.Equal("gzip", transmission.ContentEncoding);
                Assert.Equal(string.Empty, Unzip(transmission.Content));
            }

            [TestMethod]
            public void DoesNotContinueAsyncOperationsOnCapturedSynchronizationContextToImprovePerformance()
            {
                var transmitter = new StubTransmitter() { OnEnqueue = transmission => TaskEx.Run(() => { }) };
                var serializer = new TelemetrySerializer(transmitter);

                bool postedBack = false;
                using (var context = new StubSynchronizationContext())
                {
                    context.OnPost = (callback, state) =>
                    {
                        postedBack = true;
                        callback(state);
                    };

                    serializer.Serialize(new[] { new StubTelemetry() });
                }

                Assert.False(postedBack);
            }

            private static string Unzip(byte[] content)
            {
                var memoryStream = new MemoryStream(content);
                var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
                using (var streamReader = new StreamReader(gzipStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
