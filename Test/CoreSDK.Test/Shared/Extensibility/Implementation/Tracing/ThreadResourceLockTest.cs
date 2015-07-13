namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System.Threading;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

    [TestClass]
    public class ThreadResourceLockTest
    {
        [TestMethod]
        public void LockIsSetOnSameThread()
        {
            Assert.IsFalse(ThreadResourceLock.IsResourceLocked);
            using (var resourceLock = new ThreadResourceLock())
            {
                Assert.IsTrue(ThreadResourceLock.IsResourceLocked);
            }

            Assert.IsFalse(ThreadResourceLock.IsResourceLocked);
        }

#if NET40
        [TestMethod]
        public void LockNotSetOnDifferentThread()
        {
            Assert.IsFalse(ThreadResourceLock.IsResourceLocked);
            using (var resourceLock = new ThreadResourceLock())
            {
                Assert.IsTrue(ThreadResourceLock.IsResourceLocked);
                var otherThread = new Thread(new ThreadStart(() =>
                {
                    Assert.IsFalse(ThreadResourceLock.IsResourceLocked);
                    using (var internalResourceLock = new ThreadResourceLock())
                    {
                        Assert.IsTrue(ThreadResourceLock.IsResourceLocked);
                    }

                    Assert.IsFalse(ThreadResourceLock.IsResourceLocked);
                }));
                otherThread.Start();

                otherThread.Join();
                Assert.IsTrue(ThreadResourceLock.IsResourceLocked);
            }
        }
#endif
    }
}
