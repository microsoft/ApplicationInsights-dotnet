namespace Microsoft.ApplicationInsights.PersistenceChannel.Net40.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class SenderTests
    {
        private int deleteCount;

        private Mock<StorageTransmission> TransmissionMock { get; set; }

        private Mock<StorageBase> StorageBaseMock { get; set; }

        private SenderUnderTest Sender { get; set; }

        [TestInitialize]
        public void Setup()
        {
            this.StorageBaseMock = new Moq.Mock<StorageBase>();
            this.TransmissionMock = new Moq.Mock<StorageTransmission>(string.Empty, new Uri("http://some/url"), new byte[] { }, string.Empty, string.Empty);
            var transmitter = new PersistenceTransmitter(new Storage(string.Empty), 0);           
            this.Sender = new SenderUnderTest(this.StorageBaseMock.Object, transmitter);
            this.deleteCount = 0;
            this.StorageBaseMock.Setup((storage) => storage.Delete(It.IsAny<StorageTransmission>()))
                .Callback(() => this.deleteCount++);
        }

        [TestMethod]
        public void WhenServerReturn429ExponentialBackoffPatternIsUsedButFailedTelemetriesAreDropped()
        {
            int peekCounts = 0;

            // Setup transmission.SendAsync() to throw WebException that has 429 status Code
            this.TransmissionMock.Setup(transmission => transmission.SendAsync()).Throws(this.GenerateWebException((HttpStatusCode)429));

            // Setup Storage.Peek() to return the mocked transmission, and stop the loop after 10 peeks.
            this.StorageBaseMock.Setup((storage) => storage.Peek())
                .Returns(this.TransmissionMock.Object)
                .Callback(() =>
                {
                    if (peekCounts++ == 10)
                    {
                        this.Sender.StopAsync();
                    }
                });

            // save interval on every loop.
            List<TimeSpan> actualIntervals = new List<TimeSpan>();
            this.Sender.OnSend = (TimeSpan interval) => actualIntervals.Add(interval);

            // Act 
            this.Sender.SendLoop();

            // Asserts
            Assert.AreEqual(10, this.deleteCount);

            int cur = 10;
            for (int i = 0; i < 9; i++)
            {
                Assert.AreEqual(cur, actualIntervals[i].TotalSeconds);
                cur *= 2;
            }

            // the last one is already reached the maximum of 1 hour.
            Assert.AreEqual(TimeSpan.FromHours(1).TotalSeconds, actualIntervals[9].TotalSeconds);
        }

        /// <summary>
        /// Check that after 6 (a random number between 1 to 10) calls to peek the sending interval is 10.
        /// </summary>
        [TestMethod]
        public void WhenPeekReturnNullSendingIntervalIs6()
        {
            int peekCounts = 0;

            // Setup Storage.Peek() to return a Transmission only on try number 6 and stop the loop at the 7th peek. 
            this.StorageBaseMock.Setup((storage) => storage.Peek())
                .Returns(() =>
                {
                    this.Sender.IntervalAutoResetEvent.Set();
                    if (peekCounts == 6)
                    {
                        return this.TransmissionMock.Object;
                    }

                    return null;
                })
                .Callback(() =>
                {
                    if (peekCounts++ == 7)
                    {
                        this.Sender.StopAsync();
                    }
                });

            // Cache the interval (it is a parameter passed to the Send method) on the 6th try.
            TimeSpan intervalOnSixIteration = TimeSpan.Zero;
            this.Sender.OnSend = (TimeSpan interval) => intervalOnSixIteration = interval;

            // Act 
            this.Sender.SendLoop();

            Assert.AreEqual(10, intervalOnSixIteration.TotalSeconds);
        }

        [TestMethod]
        public void WhenServerReturn503TransmissionWillBeRetried()
        {
            int peekCounts = 0;

            // Setup transmission.SendAsync() to throw WebException that has 503 status Code
            this.TransmissionMock.Setup(transmission => transmission.SendAsync()).Throws(this.GenerateWebException((HttpStatusCode)503));

            // Setup Storage.Peek() to return the mocked transmission, and stop the loop after 10 peeks.
            this.StorageBaseMock.Setup((storage) => storage.Peek())
                .Returns(this.TransmissionMock.Object)
                .Callback(() =>
                {
                    if (peekCounts++ == 10)
                    {
                        this.Sender.StopAsync();
                    }
                });

            // Act 
            this.Sender.SendLoop();

            Assert.AreEqual(0, this.deleteCount, "delete is not expected to be called on 503, request is expected to be send forever.");
        }

        [TestMethod]
        public void WhenServerReturn400IntervalWillBe10Seconds()
        {
            int peekCounts = 0;

            // Setup transmission.SendAsync() to throw WebException that has 400 status Code
            this.TransmissionMock.Setup(transmission => transmission.SendAsync()).Throws(this.GenerateWebException((HttpStatusCode)400));

            // Setup Storage.Peek() to return the mocked transmission, and stop the loop after 10 peeks.
            this.StorageBaseMock.Setup((storage) => storage.Peek())
                .Returns(this.TransmissionMock.Object)
                .Callback(() =>
                {
                    if (peekCounts++ == 10)
                    {
                        this.Sender.StopAsync();
                    }
                });

            // Cache the interval (it is a parameter passed to the Send method).
            TimeSpan intervalOnSixIteration = TimeSpan.Zero;
            this.Sender.OnSend = (TimeSpan interval) => intervalOnSixIteration = interval;

            // Act 
            this.Sender.SendLoop();

            Assert.AreEqual(5, intervalOnSixIteration.TotalSeconds);
            Assert.AreEqual(10, this.deleteCount, "400 should not be retried so delete should always be called.");
        }

        [TestMethod]
        public void DisposeDoesNotThrow()
        {
            new Sender(this.StorageBaseMock.Object, new PersistenceTransmitter(new Storage(string.Empty), 3)).Dispose();
        }

        [TestMethod]
        public void WhenServerReturnDnsErrorRequestWillBeRetried()
        {
            int peekCounts = 0;

            // Setup transmission.SendAsync() to throw WebException with ProxyNameResolutionFailure failure
            var webException = new WebException(string.Empty, new Exception(), WebExceptionStatus.ProxyNameResolutionFailure, null);
            this.TransmissionMock.Setup(transmission => transmission.SendAsync()).Throws(webException);

            // Setup Storage.Peek() to return the mocked transmission, and stop the loop after 10 peeks.
            this.StorageBaseMock.Setup((storage) => storage.Peek())
                .Returns(this.TransmissionMock.Object)
                .Callback(() =>
                {
                    if (peekCounts++ == 10)
                    {
                        this.Sender.StopAsync();
                    }
                });

            // Act 
            this.Sender.SendLoop();

            Assert.AreEqual(0, this.deleteCount, "delete is not expected to be called on Dns errors since it , request is expected to be retried forever.");
        }

        private WebException GenerateWebException(HttpStatusCode httpStatusCode)
        {
            var httpWebResponse = new Mock<HttpWebResponse>();
            httpWebResponse.SetupGet(webResponse => webResponse.StatusCode).Returns(httpStatusCode);

            var webException = new WebException(string.Empty, new Exception(), WebExceptionStatus.SendFailure, httpWebResponse.Object);

            return webException;
        }

        /// <summary>
        /// A class that inherits from Sender, to expose its protected methods. 
        /// </summary>
        internal class SenderUnderTest : Sender
        {
            internal Action<TimeSpan> OnSend = (TimeSpan nextSendInterval) => { };

            internal SenderUnderTest(StorageBase storage, PersistenceTransmitter transmitter)
                : base(storage, transmitter, startSending: false)
            {
            }

            internal AutoResetEvent IntervalAutoResetEvent
            {
                get
                {
                    return this.DelayHandler;
                }
            }

            internal new void SendLoop()
            {
                base.SendLoop();
            }

            protected override bool Send(StorageTransmission transmission, ref TimeSpan nextSendInterval)
            {
                this.OnSend(nextSendInterval);
                this.DelayHandler.Set();
                return base.Send(transmission, ref nextSendInterval);
            }
        }
    }
}
