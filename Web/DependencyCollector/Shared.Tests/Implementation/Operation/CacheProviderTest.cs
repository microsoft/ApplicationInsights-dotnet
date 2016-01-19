namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation
{
    using System;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CacheProviderTest
    {
        [TestMethod]
        public void TestContains()
        {
            using (var cacheProvider = new CacheProvider<string>((int)TimeSpan.FromSeconds(60).TotalMilliseconds))
            {
                const int Key = 123;
                cacheProvider.Set(Key, "aValue1");
                Assert.IsTrue(cacheProvider.Contains(Key));
                cacheProvider.Set(Key, "aKey2");
                Assert.IsTrue(cacheProvider.Contains(Key));
            }
        }

        [TestMethod]
        public void TestReset()
        {
            using (var cacheProvider = new CacheProvider<string>((int)TimeSpan.FromSeconds(60).TotalMilliseconds))
            {
                const int Key = 123;
                cacheProvider.Set(Key, "aValue1");
                cacheProvider.Set(Key, "aKey2");
                Assert.IsTrue(cacheProvider.Contains(Key));
            }
        }

        [TestMethod]
        public void TestGet()
        {
            using (var cacheProvider = new CacheProvider<string>((int)TimeSpan.FromSeconds(60).TotalMilliseconds))
            {
                const int Key = 123;
                const string ExpectedValue = "aValue1";
                cacheProvider.Set(Key, ExpectedValue);
                var value = cacheProvider.Get(Key);
                Assert.AreEqual(ExpectedValue, value);
            }
        }

        [TestMethod]
        public void TestItemExpiration()
        {
            using (var cacheProvider = new CacheProvider<string>((int)TimeSpan.FromMilliseconds(200).TotalMilliseconds))
            {
                const int Key = 123;
                cacheProvider.Set(Key, "value");
                Thread.Sleep(2000);
                var value = cacheProvider.Get(Key);
                Assert.IsNull(value, "item must be expired and removed from the cache");
            }
        }

        [TestMethod]
        public void TestRemove()
        {
            using (var cacheProvider = new CacheProvider<string>((int)TimeSpan.FromSeconds(60).TotalMilliseconds))
            {
                const int Key = 123;
                const string ExpectedValue = "aValue1";
                cacheProvider.Set(Key, ExpectedValue);
                cacheProvider.Remove(Key);
                var value = cacheProvider.Get(Key);
                Assert.IsNull(value);
            }
        }
    }
}
