namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseCollectionStateManagerTests
    {
        private const string StartCollectionMessage = "StartCollection";

        private const string StopCollectionMessage = "StopCollection";

        private const string CollectMessage = "Collect";

        [TestMethod]
        public void QuickPulseCollectionStateManagerDoesNothingWithoutInstrumentationKey()
        {
            // ARRANGE
            var timings = QuickPulseTimings.Default;
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var actions = new List<string>();
            var returnedSamples = new List<QuickPulseDataSample>();
            var timeProvider = new ClockMock();
            var manager = CreateManager(serviceClient, timeProvider, actions, returnedSamples, timings);

            // ACT
            manager.UpdateState(string.Empty);
            manager.UpdateState(null);

            // ASSERT
            Assert.AreEqual(0, serviceClient.PingCount);
            Assert.AreEqual(0, serviceClient.SnappedSamples.Count);
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerInitiallyInIdleState()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock();

            var manager = new QuickPulseCollectionStateManager(
                serviceClient,
                new Clock(),
                QuickPulseTimings.Default,
                () => { },
                () => { },
                () => null,
                _ => { });

            // ACT

            // ASSERT
            Assert.AreEqual(false, manager.IsCollectingData);
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerStaysInIdleState()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = false };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, new Clock(), actions);

            // ACT
            for (int i = 0; i < 10; i++)
            {
                manager.UpdateState("empty iKey");
            }

            // ASSERT
            Assert.AreEqual(false, manager.IsCollectingData);
            Assert.AreEqual(0, actions.Count);
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerTransitionsFromIdleToCollect()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, new Clock(), actions);

            // ACT
            manager.UpdateState("empty iKey");

            // on the next call it actually requests samples collected since the last call
            manager.UpdateState("empty iKey");

            // ASSERT
            Assert.AreEqual(true, manager.IsCollectingData);
            Assert.AreEqual(2, actions.Count);
            Assert.AreEqual(StartCollectionMessage, actions[0]);
            Assert.AreEqual(CollectMessage, actions[1]);
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerTransitionsFromIdleToCollectAndImmediatelyBack()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = false };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, new Clock(), actions);

            // ACT
            manager.UpdateState("empty iKey");

            // requests samples collected since last call - and puts itself back into the idle state
            manager.UpdateState("empty iKey");

            // ASSERT
            Assert.AreEqual(false, manager.IsCollectingData);
            Assert.AreEqual(3, actions.Count);
            Assert.AreEqual(StartCollectionMessage, actions[0]);
            Assert.AreEqual(CollectMessage, actions[1]);
            Assert.AreEqual(StopCollectionMessage, actions[2]);
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerTransitionsFromIdleToCollectAndStaysStable()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, new Clock(), actions);

            // ACT
            manager.UpdateState("empty iKey");

            // now we start sending samples
            int collectionCount = 10;
            for (int i = 0; i < collectionCount; i++)
            {
                manager.UpdateState("empty iKey");
            }

            // ASSERT
            Assert.AreEqual(true, manager.IsCollectingData);
            Assert.AreEqual(1 + collectionCount, actions.Count);
            Assert.AreEqual(StartCollectionMessage, actions[0]);
            for (int i = 1; i < collectionCount; i++)
            {
                Assert.AreEqual(CollectMessage, actions[i]);
            }
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerTransitionsFromCollectToIdleAndStaysStable()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, new Clock(), actions);

            // enter collect state
            manager.UpdateState("empty iKey");

            serviceClient.ReturnValueFromPing = false;
            serviceClient.ReturnValueFromSubmitSample = false;

            actions.Clear();

            // ACT
            int collectionCount = 10;
            for (int i = 0; i < collectionCount; i++)
            {
                manager.UpdateState("empty iKey");
            }

            // ASSERT
            Assert.AreEqual(false, manager.IsCollectingData);
            Assert.AreEqual(2, actions.Count);
            Assert.AreEqual(CollectMessage, actions[0]);
            Assert.AreEqual(StopCollectionMessage, actions[1]);
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerFlipFlopsBetweenIdleAndCollect()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = false };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, new Clock(), actions);

            // ACT
            int collectionCount = 10;
            for (int i = 0; i < collectionCount; i++)
            {
                manager.UpdateState("empty iKey");
            }

            // ASSERT
            Assert.AreEqual(false, manager.IsCollectingData);
            Assert.AreEqual(1.5 * collectionCount, actions.Count);
            for (int i = 0; i < collectionCount; i += 3)
            {
                Assert.AreEqual(StartCollectionMessage, actions[i + 0]);

                Assert.AreEqual(CollectMessage, actions[i + 1]);
                Assert.AreEqual(StopCollectionMessage, actions[i + 2]);
            }
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerFlipFlopsBetweenCollectAndIdle()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, new Clock(), actions);

            // enter collect state
            manager.UpdateState("empty iKey");

            actions.Clear();

            // ACT
            int collectionCount = 10;
            for (int i = 0; i < collectionCount; i++)
            {
                if (i % 2 == 0)
                {
                    // collect -> idle
                    serviceClient.ReturnValueFromPing = false;
                    serviceClient.ReturnValueFromSubmitSample = false;
                }
                else
                {
                    // idle -> collect
                    serviceClient.ReturnValueFromPing = true;
                    serviceClient.ReturnValueFromSubmitSample = true;
                }

                manager.UpdateState("empty iKey");
            }

            // ASSERT
            Assert.AreEqual(true, manager.IsCollectingData);

            Assert.AreEqual(1.5 * collectionCount, actions.Count);
            for (int i = 0; i < 1.5 * collectionCount; i += 3)
            {
                Assert.AreEqual(CollectMessage, actions[i + 0]);
                Assert.AreEqual(StopCollectionMessage, actions[i + 1]);
                Assert.AreEqual(StartCollectionMessage, actions[i + 2]);
            }
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerReturnsFailedSamplesBack()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var actions = new List<string>();
            var returnedSamples = new List<QuickPulseDataSample>();
            var manager = CreateManager(serviceClient, new Clock(), actions, returnedSamples);

            // turn on collection
            manager.UpdateState("empty iKey");

            // ACT
            // lost connection
            serviceClient.ReturnValueFromSubmitSample = null;

            manager.UpdateState("empty iKey");

            // ASSERT
            Assert.AreEqual(1, returnedSamples.Count);
            Assert.AreEqual(5, returnedSamples[0].AIRequestsSucceededPerSecond);
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerPingBacksOff()
        {
            // ARRANGE
            var timings = QuickPulseTimings.Default;
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = false };
            var actions = new List<string>();
            var returnedSamples = new List<QuickPulseDataSample>();
            var timeProvider = new ClockMock();
            var manager = CreateManager(serviceClient, timeProvider, actions, returnedSamples, timings);

            // ACT & ASSERT
            Assert.AreEqual(timings.ServicePollingInterval, manager.UpdateState("some ikey"));

            serviceClient.ReturnValueFromPing = null;
            timeProvider.FastForward(timings.TimeToServicePollingBackOff.Add(TimeSpan.FromSeconds(-1)));
            Assert.AreEqual(timings.ServicePollingInterval, manager.UpdateState("some ikey"));

            timeProvider.FastForward(TimeSpan.FromSeconds(2));
            Assert.AreEqual(timings.ServicePollingBackedOffInterval, manager.UpdateState("some ikey"));
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerPingBacksOffWhenConnectionInitiallyDown()
        {
            // ARRANGE
            var timings = QuickPulseTimings.Default;
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = null, ReturnValueFromSubmitSample = null };
            var actions = new List<string>();
            var returnedSamples = new List<QuickPulseDataSample>();
            var timeProvider = new ClockMock();
            var manager = CreateManager(serviceClient, timeProvider, actions, returnedSamples, timings);

            // ACT & ASSERT
            manager.UpdateState("some ikey");

            timeProvider.FastForward(timings.TimeToServicePollingBackOff.Add(TimeSpan.FromSeconds(-1)));
            Assert.AreEqual(timings.ServicePollingInterval, manager.UpdateState("some ikey"));

            timeProvider.FastForward(TimeSpan.FromSeconds(2));
            Assert.AreEqual(timings.ServicePollingBackedOffInterval, manager.UpdateState("some ikey"));
        }
        
        [TestMethod]
        public void QuickPulseCollectionStateManagerPingDoesNotBackOffOnFirstPing()
        {
            // ARRANGE
            var timings = QuickPulseTimings.Default;
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = false };
            var actions = new List<string>();
            var returnedSamples = new List<QuickPulseDataSample>();
            var timeProvider = new ClockMock();
            var manager = CreateManager(serviceClient, timeProvider, actions, returnedSamples, timings);

            // ACT
            serviceClient.ReturnValueFromPing = null;
            timeProvider.FastForward(timings.TimeToServicePollingBackOff.Add(TimeSpan.FromSeconds(1)));

            // ASSERT
            Assert.AreEqual(timings.ServicePollingInterval, manager.UpdateState(string.Empty));
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerPingRecovers()
        {
            // ARRANGE
            var timings = QuickPulseTimings.Default;
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = false };
            var actions = new List<string>();
            var returnedSamples = new List<QuickPulseDataSample>();
            var timeProvider = new ClockMock();
            var manager = CreateManager(serviceClient, timeProvider, actions, returnedSamples, timings);

            Assert.AreEqual(timings.ServicePollingInterval, manager.UpdateState(string.Empty));

            // ACT
            serviceClient.ReturnValueFromPing = null;
            timeProvider.FastForward(timings.TimeToServicePollingBackOff.Add(TimeSpan.FromSeconds(1)));
            manager.UpdateState(string.Empty);

            timeProvider.FastForward(TimeSpan.FromMinutes(1));
            serviceClient.ReturnValueFromPing = false;

            // ASSERT
            Assert.AreEqual(timings.ServicePollingInterval, manager.UpdateState(string.Empty));
        }
        
        [TestMethod]
        public void QuickPulseCollectionStateManagerSubmitBacksOff()
        {
            // ARRANGE
            var timings = QuickPulseTimings.Default;
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var actions = new List<string>();
            var returnedSamples = new List<QuickPulseDataSample>();
            var timeProvider = new ClockMock();
            var manager = CreateManager(serviceClient, timeProvider, actions, returnedSamples, timings);

            manager.UpdateState(string.Empty);

            // ACT & ASSERT
            Assert.AreEqual(timings.CollectionInterval, manager.UpdateState("some ikey"));

            serviceClient.ReturnValueFromSubmitSample = null;
            timeProvider.FastForward(timings.TimeToCollectionBackOff.Add(TimeSpan.FromSeconds(-1)));
            Assert.AreEqual(timings.CollectionInterval, manager.UpdateState("some ikey"));
            Assert.AreEqual(true, manager.IsCollectingData);

            timeProvider.FastForward(TimeSpan.FromSeconds(2));
            Assert.AreEqual(timings.ServicePollingBackedOffInterval, manager.UpdateState("some ikey"));
            Assert.AreEqual(false, manager.IsCollectingData);
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerSubmitBacksOffWhenConnectionInitiallyDown()
        {
            // ARRANGE
            var timings = QuickPulseTimings.Default;
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = null };
            var actions = new List<string>();
            var returnedSamples = new List<QuickPulseDataSample>();
            var timeProvider = new ClockMock();
            var manager = CreateManager(serviceClient, timeProvider, actions, returnedSamples, timings);

            // ACT & ASSERT
            Assert.AreEqual(timings.CollectionInterval, manager.UpdateState("some ikey"));
            Assert.IsTrue(manager.IsCollectingData);

            timeProvider.FastForward(timings.TimeToCollectionBackOff.Add(TimeSpan.FromSeconds(-1)));
            Assert.AreEqual(timings.CollectionInterval, manager.UpdateState("some ikey"));
            Assert.IsTrue(manager.IsCollectingData);

            timeProvider.FastForward(TimeSpan.FromSeconds(2));
            Assert.AreEqual(timings.ServicePollingBackedOffInterval, manager.UpdateState("some ikey"));
            Assert.AreEqual(false, manager.IsCollectingData);
        }

        #region Helpers

        private static QuickPulseCollectionStateManager CreateManager(
            IQuickPulseServiceClient serviceClient,
            Clock timeProvider,
            List<string> actions,
            List<QuickPulseDataSample> returnedSamples = null,
            QuickPulseTimings timings = null)
        {
            var manager = new QuickPulseCollectionStateManager(
                serviceClient,
                timeProvider,
                timings ?? QuickPulseTimings.Default,
                () => actions.Add(StartCollectionMessage),
                () => actions.Add(StopCollectionMessage),
                () =>
                    {
                        actions.Add(CollectMessage);

                        var now = DateTimeOffset.UtcNow;
                        return
                            new[]
                                {
                                    new QuickPulseDataSample(
                                        new QuickPulseDataAccumulator
                                            {
                                                AIRequestSuccessCount = 5,
                                                StartTimestamp = now,
                                                EndTimestamp = now.AddSeconds(1)
                                            },
                                        new Dictionary<string, Tuple<PerformanceCounterData, double>>(),
                                        Enumerable.Empty<Tuple<string, int>>(),
                                        false)
                                }.ToList();
                    },
                samples =>
                    {
                        if (returnedSamples != null)
                        {
                            returnedSamples.AddRange(samples);
                        }
                    });

            return manager;
        }

        #endregion
    }
}