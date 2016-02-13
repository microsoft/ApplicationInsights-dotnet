namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseCollectionStateManagerTests
    {
        const string StartCollectionMessage = "StartCollection";
        const string StopCollectionMessage = "StopCollection";
        const string CollectMessage = "Collect";
        
        [TestMethod]
        public void QuickPulseCollectionStateManagerInitiallyInIdleState()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock();

            var manager = new QuickPulseCollectionStateManager(serviceClient, () => { }, () => { }, () => null, _ => { });

            // ACT
            
            // ASSERT
            Assert.AreEqual(false, manager.IsCollectingData);
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerStaysInIdleState()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock
            {
                ReturnValueFromPing = false,
                ReturnValueFromSubmitSample = false
            };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, actions);

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
            var serviceClient = new QuickPulseServiceClientMock
            {
                ReturnValueFromPing = true,
                ReturnValueFromSubmitSample = true
            };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, actions);

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
            var serviceClient = new QuickPulseServiceClientMock
            {
                ReturnValueFromPing = true,
                ReturnValueFromSubmitSample = false
            };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, actions);

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
            var serviceClient = new QuickPulseServiceClientMock
            {
                ReturnValueFromPing = true,
                ReturnValueFromSubmitSample = true
            };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, actions);

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
            var serviceClient = new QuickPulseServiceClientMock
            {
                ReturnValueFromPing = true,
                ReturnValueFromSubmitSample = true
            };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, actions);

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
            var serviceClient = new QuickPulseServiceClientMock
            {
                ReturnValueFromPing = true,
                ReturnValueFromSubmitSample = false
            };

            var actions = new List<string>();
            var manager = CreateManager(serviceClient, actions);
            
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
            var manager = CreateManager(serviceClient, actions);

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
            var manager = CreateManager(serviceClient, actions, returnedSamples);

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

        private static QuickPulseCollectionStateManager CreateManager(
            QuickPulseServiceClientMock serviceClient,
            List<string> actions,
            List<QuickPulseDataSample> returnedSamples = null)
        {
            var manager = new QuickPulseCollectionStateManager(
                serviceClient,
                () => actions.Add(StartCollectionMessage),
                () => actions.Add(StopCollectionMessage),
                () =>
                    {
                        actions.Add(CollectMessage);

                        var now = DateTime.UtcNow;
                        return
                            new[]
                                {
                                    new QuickPulseDataSample(
                                        new QuickPulseDataAccumulator { AIRequestSuccessCount = 5, StartTimestamp = now, EndTimestamp = now.AddSeconds(1) },
                                        new Dictionary<string, float>())
                                }.ToList();
                    },
                samples => { returnedSamples?.AddRange(samples); });

            return manager;
        }
    }
}
