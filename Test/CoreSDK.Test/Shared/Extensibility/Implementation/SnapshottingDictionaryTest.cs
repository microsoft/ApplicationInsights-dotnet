// <copyright file="SnapshottingDictionaryTest.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;

#if NET40 || NET45 || NET35 || NET46
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using Assert = Xunit.Assert;
#if WINRT
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public class SnapshottingDictionaryTest
    {
        [TestClass]
        public class Class : SnapshottingDictionaryTest
        {
            [TestMethod]
            public void IsInternalAndNotMeantForPublicConsumption()
            {
                Assert.False(typeof(SnapshottingDictionary<,>).GetTypeInfo().IsPublic);
            }

            [TestMethod]
            public void ImplementIDictionaryInterfaceForCompatibilityWithPublicApis()
            {
                Assert.True(typeof(IDictionary<object, object>).IsAssignableFrom(typeof(SnapshottingDictionary<object, object>)));
            }
        }

        [TestClass]
        public class Constructor : SnapshottingDictionaryTest
        {
            [TestMethod]
            public void CreatesNewDictionary()
            {
                var target = new TestableSnapshottingDictionary<object, object>();
                Assert.NotNull(target.Collection);
            }
        }

        [TestClass]
        public class CreateSnapshot : SnapshottingDictionaryTest
        {
            [TestMethod]
            public void CreatesClonesGivenDictionary()
            {
                var target = new TestableSnapshottingDictionary<object, object>();

                var key = new object();
                var value = new object();
                var input = new Dictionary<object, object> { { key, value } };
                IDictionary<object, object> output = target.CreateSnapshot(input);

                Assert.Same(value, output[key]);
            }
        }

        [TestClass]
        public class Add : SnapshottingDictionaryTest
        {
            [TestMethod]
            public void AddsItemWithGivenKeyInDictionary()
            {
                var target = new TestableSnapshottingDictionary<object, object>();

                var key = new object();
                var value = new object();
                target.Add(key, value);

                Assert.Same(value, target.Collection[key]);
            }

            [TestMethod]
            public void ResetsSnapshotSoThatItIsRecreatedAtNextRead()
            {
                var target = new TestableSnapshottingDictionary<object, object>();
                target.GetSnapshot();

                target.Add(new object(), new object());

                Assert.Null(target.Snapshot);
            }

            [TestMethod]
            public void LocksCollectionForThreadSafety()
            {
                Task anotherThread;
                var target = new TestableSnapshottingDictionary<object, object>();
                lock (target.Collection)
                {
                    anotherThread = TaskEx.Run(() => target.Add(new object(), new object()));
                    Assert.False(anotherThread.Wait(20));
                }

                Assert.True(anotherThread.Wait(20));
            }
        }

        [TestClass]
        public class ContainsKey : SnapshottingDictionaryTest
        {
            [TestMethod]
            public void ReturnsTrueIfSnapshotContainsGivenKey()
            {
                var target = new TestableSnapshottingDictionary<object, object>();
                var key = new object();
                target.Snapshot = new Dictionary<object, object> { { key, null } };

                Assert.True(target.ContainsKey(key));
            }
        }

        [TestClass]
        public class Remove : SnapshottingDictionaryTest
        {
            [TestMethod]
            public void RemovesItemWithGivenKeyFromDictionary()
            {
                var key = new object();
                var target = new TestableSnapshottingDictionary<object, object> { { key, null } };

                target.Remove(key);

                Assert.Equal(0, target.Collection.Count);
            }

            [TestMethod]
            public void ReturnsTrueIfItemWasRemovedSuccessfully()
            {
                var key = new object();
                var target = new TestableSnapshottingDictionary<object, object> { { key, null } };

                Assert.True(target.Remove(key));
            }

            [TestMethod]
            public void ResetsSnapshotSoThatItIsRecreatedAtNextRead()
            {
                var key = new object();
                var target = new TestableSnapshottingDictionary<object, object> { { key, null } };
                target.GetSnapshot();

                target.Remove(key);

                Assert.Null(target.Snapshot);
            }

            [TestMethod]
            public void DoesNotResetSnapshotWhenItemWithGivenKeyWasNotRemoved()
            {
                var target = new TestableSnapshottingDictionary<object, object>();
                IDictionary<object, object> oldSnapshot = target.GetSnapshot();

                target.Remove(new object());

                Assert.Same(oldSnapshot, target.Snapshot);
            }

            [TestMethod]
            public void LocksCollectionForThreadSafety()
            {
                Task anotherThread;
                var key = new object();
                var target = new TestableSnapshottingDictionary<object, object> { { key, null } };
                lock (target.Collection)
                {
                    anotherThread = TaskEx.Run(() => target.Remove(key));
                    Assert.False(anotherThread.Wait(20));
                }

                Assert.True(anotherThread.Wait(20));
            }
        }

        [TestClass]
        public class TryGetValue : SnapshottingDictionaryTest
        {
            [TestMethod]
            public void ReturnsValueWithGivenKeyInSnapshot()
            {
                var target = new TestableSnapshottingDictionary<object, object>();
                var key = new object();
                var value = new object();
                target.Snapshot = new Dictionary<object, object> { { key, value } };

                object returnedValue = null;
                bool result = target.TryGetValue(key, out returnedValue);

                Assert.True(result);
                Assert.Same(value, returnedValue);
            }
        }

        [TestClass]
        public class Keys : SnapshottingDictionaryTest
        {
            [TestMethod]
            public void ReturnsKeysOfSnapshot()
            {
                var target = new TestableSnapshottingDictionary<object, object>();
                var key = new object();
                target.Snapshot = new Dictionary<object, object> { { key, null } };

                Assert.True(target.Keys.Contains(key));
            }
        }

        [TestClass]
        public class Values : SnapshottingDictionaryTest
        {
            [TestMethod]
            public void ReturnsValuesOfSnapshot()
            {
                var target = new TestableSnapshottingDictionary<object, object>();
                var value = new object();
                target.Snapshot = new Dictionary<object, object> { { new object(), value } };

                Assert.True(target.Values.Contains(value));
            }
        }

        [TestClass]
        public class Item : SnapshottingDictionaryTest
        {
            [TestMethod]
            public void GetterReturnsValueWithGivenKeyInSnapshot()
            {
                var target = new TestableSnapshottingDictionary<object, object>();
                var key = new object();
                var value = new object();
                target.Snapshot = new Dictionary<object, object> { { key, value } };

                Assert.Same(value, target[key]);
            }

            [TestMethod]
            public void SetterReplacesItemWithGivenKeyInDictionary()
            {
                var key = new object();
                var target = new TestableSnapshottingDictionary<object, object> { { key, null } };

                var value = new object();
                target[key] = value;

                Assert.Same(value, target.Collection[key]);
            }

            [TestMethod]
            public void SetterResetsSnapshotSoThatItIsRecreatedAtNextRead()
            {
                var key = new object();
                var target = new TestableSnapshottingDictionary<object, object> { { key, null } };
                target.GetSnapshot();

                target[key] = new object();

                Assert.Null(target.Snapshot);
            }

            [TestMethod]
            public void SetterLocksCollectionForThreadSafety()
            {
                Task anotherThread;
                var key = new object();
                var target = new TestableSnapshottingDictionary<object, object> { { key, null } };
                lock (target.Collection)
                {
                    anotherThread = TaskEx.Run(() => target[key] = new object());
                    Assert.False(anotherThread.Wait(20));
                }

                Assert.True(anotherThread.Wait(20));
            }
        }
        
        private class TestableSnapshottingDictionary<TKey, TValue> : SnapshottingDictionary<TKey, TValue>
        {
            public new IDictionary<TKey, TValue> Collection
            {
                get { return base.Collection; }
            }

            public IDictionary<TKey, TValue> Snapshot
            {
                get { return this.snapshot; }
                set { this.snapshot = value; }
            }

            public new IDictionary<TKey, TValue> CreateSnapshot(IDictionary<TKey, TValue> collection)
            {
                return base.CreateSnapshot(collection);
            }

            public new IDictionary<TKey, TValue> GetSnapshot()
            {
                return base.GetSnapshot();
            }
        }
    }
}
