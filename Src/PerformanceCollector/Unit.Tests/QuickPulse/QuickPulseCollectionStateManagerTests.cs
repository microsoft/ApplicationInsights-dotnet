namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseCollectionStateManagerTests
    {
        const string StartCollection = "StartCollection";
        const string StopCollection = "StopCollection";
        const string Collect = "Collect";
        
        [TestMethod]
        public void QuickPulseCollectionStateManagerInitiallyInIdleState()
        {
            // ARRANGE
            var serviceClient = new QuickPulseServiceClientMock();

            var manager = new QuickPulseCollectionStateManager(serviceClient, () => { }, () => { }, () => null);

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
                manager.PerformAction();
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
            manager.PerformAction();
            
            // ASSERT
            Assert.AreEqual(true, manager.IsCollectingData);
            Assert.AreEqual(2, actions.Count);
            Assert.AreEqual(StartCollection, actions[0]);
            Assert.AreEqual(Collect, actions[1]);
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
            manager.PerformAction();

            // ASSERT
            Assert.AreEqual(false, manager.IsCollectingData);
            Assert.AreEqual(3, actions.Count);
            Assert.AreEqual(StartCollection, actions[0]);
            Assert.AreEqual(Collect, actions[1]);
            Assert.AreEqual(StopCollection, actions[2]);
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
            int collectionCount = 10;
            for (int i = 0; i < collectionCount; i++)
            {
                manager.PerformAction();
            }

            // ASSERT
            Assert.AreEqual(true, manager.IsCollectingData);
            Assert.AreEqual(1 + collectionCount, actions.Count);
            Assert.AreEqual(StartCollection, actions[0]);
            for (int i = 1; i < collectionCount; i++)
            {
                Assert.AreEqual(Collect, actions[i]);
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
            manager.PerformAction();

            serviceClient.ReturnValueFromPing = false;
            serviceClient.ReturnValueFromSubmitSample = false;

            actions.Clear();

            // ACT
            int collectionCount = 10;
            for (int i = 0; i < collectionCount; i++)
            {
                manager.PerformAction();
            }
            
            // ASSERT
            Assert.AreEqual(false, manager.IsCollectingData);
            Assert.AreEqual(2, actions.Count);
            Assert.AreEqual(Collect, actions[0]);
            Assert.AreEqual(StopCollection, actions[1]);
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
                manager.PerformAction();
            }

            // ASSERT
            Assert.AreEqual(false, manager.IsCollectingData);
            Assert.AreEqual(3 * collectionCount, actions.Count);
            for (int i = 0; i < collectionCount; i += 3)
            {
                Assert.AreEqual(StartCollection, actions[i + 0]);
                Assert.AreEqual(Collect, actions[i + 1]);
                Assert.AreEqual(StopCollection, actions[i + 2]);
            }
        }

        [TestMethod]
        public void QuickPulseCollectionStateManagerFlipFlopsBetweenCollectAndIdle()
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
            manager.PerformAction();

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

                manager.PerformAction();
            }

            // ASSERT
            Assert.AreEqual(true, manager.IsCollectingData);

            Assert.AreEqual(2 * collectionCount, actions.Count);
            for (int i = 0; i < 2 * collectionCount; i += 2)
            {
                if (i % 4 == 0)
                {
                    Assert.AreEqual(Collect, actions[i + 0]);
                    Assert.AreEqual(StopCollection, actions[i + 1]);
                }
                else
                {
                    Assert.AreEqual(StartCollection, actions[i + 0]);
                    Assert.AreEqual(Collect, actions[i + 1]);
                }
            }
        }

        private static QuickPulseCollectionStateManager CreateManager(QuickPulseServiceClientMock serviceClient, List<string> actions)
        {
            var manager = new QuickPulseCollectionStateManager(
                serviceClient,
                () => actions.Add(StartCollection),
                () => actions.Add(StopCollection),
                () =>
                {
                    actions.Add(Collect);

                    return null;
                });

            return manager;
        }
    }
}
