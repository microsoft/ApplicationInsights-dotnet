namespace Microsoft.ApplicationInsights.PersistenceChannel.Net40.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// Tests for <see cref="FlushManager" />.
    /// </summary>
    [TestClass]
    public class FlushManagerTests
    {
        private int numberOfCallsToStorageEnqueue = 0;

        private Mock<StorageBase> StorageBaseMock { get; set; }
        
        private Action OnEnqueue { get; set; } 
        
        private TelemetryBuffer TelemetryBufferMock { get; set; }

        [TestInitialize]
        public void Setup()
        {
            // Create a Storage Mock
            this.StorageBaseMock = new Moq.Mock<StorageBase>();
            this.OnEnqueue = null;

            // Setup  Storage.EnqueueAsync to call OnEnqueue and increase this.numberOfCallsToStorageEnqueue by 1
            this.StorageBaseMock.Setup((storage) => storage.EnqueueAsync(It.IsAny<Transmission>()))
                .Callback(
                () =>
                {
                    this.numberOfCallsToStorageEnqueue++;
                    if (OnEnqueue != null)
                    {
                        OnEnqueue();
                    }
                })
                .Returns(
                () =>
                {
                    var completionSource = new TaskCompletionSource<bool>();
                    completionSource.SetResult(true);
                    return completionSource.Task as Task;
                });

            // Create telemetry buffer mock
            this.TelemetryBufferMock = new TelemetryBuffer();
            this.TelemetryBufferMock.Capacity = 1;
        }

        [TestMethod]
        [Timeout(1000)]
        public void WhenEnqueueingToAFullTelemetryBufferThenFlushManagerFlushesToDisk()
        {
            // Setup
            var autoResetEvent = new AutoResetEvent(false);
            this.OnEnqueue = () => autoResetEvent.Set();            
            FlushManager flushManager = new FlushManager(this.StorageBaseMock.Object, this.TelemetryBufferMock, supportAutoFlush: true);
            flushManager.EndpointAddress = new Uri("http://some.test.com");

            // Act
            this.TelemetryBufferMock.Enqueue(new TraceTelemetry("mock_item"));
            
            // wait until Enqueue is called. 
            autoResetEvent.WaitOne();

            // Asserts
            Assert.AreEqual(1, this.numberOfCallsToStorageEnqueue);
        }

        [TestMethod]
        public void WhenEnqueueingToTelemetryBufferAndTelemetryBufferIsNotFullThenFlushManagerDoesNotFlushToDisk()
        {
            // Setup
            var autoResetEvent = new AutoResetEvent(false);
            this.OnEnqueue = () => autoResetEvent.Set();
            FlushManager flushManager = new FlushManager(this.StorageBaseMock.Object, this.TelemetryBufferMock, supportAutoFlush: false);
            flushManager.EndpointAddress = new Uri("http://some.test.com");
            this.TelemetryBufferMock.Capacity = 10;

            // Act
            this.TelemetryBufferMock.Enqueue(new TraceTelemetry("mock_item"));

            // wait for 500 milliseconds for Storage.Enqueue to be called.
            var didStorageEnqueueWasCalled = autoResetEvent.WaitOne(500);

            // Asserts
            Assert.AreEqual(false, didStorageEnqueueWasCalled);
            Assert.AreEqual(0, this.numberOfCallsToStorageEnqueue);
        }

        [TestMethod]
        [Timeout(1000)]
        public void WhenCallingFlushThenFlushManagerFlushesToDisk()
        {
            // Setup
            var autoResetEvent = new AutoResetEvent(false);
            this.OnEnqueue = () => autoResetEvent.Set();
            FlushManager flushManager = new FlushManager(this.StorageBaseMock.Object, this.TelemetryBufferMock, supportAutoFlush: true);
            flushManager.EndpointAddress = new Uri("http://some.test.com");
            this.TelemetryBufferMock.Capacity = 10;
            this.TelemetryBufferMock.Enqueue(new TraceTelemetry("mock_item"));

            // Act
            flushManager.Flush();

            // wait until Enqueue is called. 
            autoResetEvent.WaitOne();

            // Asserts
            Assert.AreEqual(1, this.numberOfCallsToStorageEnqueue);
        }
    }
}
