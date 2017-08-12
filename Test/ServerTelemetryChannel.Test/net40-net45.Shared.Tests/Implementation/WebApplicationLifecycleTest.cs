namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Threading.Tasks;
    using System.Web.Hosting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    

#if !NET40
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public sealed class WebApplicationLifecycleTest : IDisposable
    {
        public void Dispose()
        {
            StubHostingEnvironment.OnRegisterObject = obj => { };
            StubHostingEnvironment.OnUnregisterObject = obj => { };
        }

        [TestMethod]
        public void ClassIsInternalAndNotMeantForUseByCustomers()
        {
            Assert.IsFalse(typeof(WebApplicationLifecycle).IsPublic);
        }

        [TestMethod]
        public void ClassImplementsIApplicationLifecycleInterfaceToGetConsumedByCore()
        {
            Assert.IsTrue(typeof(IApplicationLifecycle).IsAssignableFrom(typeof(WebApplicationLifecycle)));
        }

        [TestMethod]
        public void ClassImplementsIRegisteredObjectInterfaceToGetConsumedByWebHostingEnvironment()
        {
            Assert.IsTrue(typeof(IRegisteredObject).IsAssignableFrom(typeof(WebApplicationLifecycle)));
        }

        [TestMethod]
        public void ClassImplementsIDisposableInterfaceToUnhookInstanceFromStaticEnvironment()
        {
            Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(typeof(WebApplicationLifecycle)));
        }

        [TestMethod]
        public void ConstructorRegistersInstanceWithWebHostingEnvironmentToReceiveShutdownNotifications()
        {
            IRegisteredObject registeredObject = null;
            StubHostingEnvironment.OnRegisterObject = obj => registeredObject = obj;
            var service = new TestableWebApplicationLifecycle(typeof(StubHostingEnvironment));
            Assert.Same(service, registeredObject);
        }

        [TestMethod]
        public void DisposeUnregistersInstanceFromWebHostingEnvironment()
        {
            IRegisteredObject unregisteredObject = null;
            StubHostingEnvironment.OnUnregisterObject = obj => unregisteredObject = obj;
            var service = new TestableWebApplicationLifecycle(typeof(StubHostingEnvironment));
            service.Dispose();
            Assert.Same(service, unregisteredObject);
        }

        [TestMethod]
        public void StopRaisesStoppingEventWhenWebHostingEnvironmentNotifiesRegisteredObjectsAboutShutdown()
        {
            var service = new WebApplicationLifecycle();

            object stoppingSender = null;
            ApplicationStoppingEventArgs stoppingArgs = null;
            service.Stopping += (sender, args) =>
            {
                stoppingSender = sender;
                stoppingArgs = args;
            };

            service.Stop(false);

            Assert.IsNotNull(stoppingSender);
            Assert.IsNotNull(stoppingArgs);
        }

        [TestMethod]
        public void StopDoesNotCrashCrashWithoutStoppingEventHandlers()
        {
            var service = new WebApplicationLifecycle();
            Exception exception = null;
            try
            {
                service.Stop(false);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsNull(exception);
        }

        [TestMethod]
        public void StopInvokesAsyncStoppingEventHandlersOnCurrentThreadToPreventHostingEnvironmentFromStoppingThreadPool()
        {
            TaskScheduler stoppingTaskScheduler = null;
            var service = new WebApplicationLifecycle();
            service.Stopping += (sender, args) =>
            {
                args.Run(() =>
                {
                    stoppingTaskScheduler = TaskScheduler.Current;
                    return TaskEx.FromResult<object>(null);
                });
            };

            service.Stop(false);

            Assert.IsType<CurrentThreadTaskScheduler>(stoppingTaskScheduler);
        }

        [TestMethod]
        public void StopDoesNotRaiseStoppingEventDuringImmediateShutdown()
        {
            var service = new WebApplicationLifecycle();

            bool eventFired = false;
            service.Stopping += (sender, args) => eventFired = true;

            service.Stop(true);

            Assert.IsFalse(eventFired);
        }

        [TestMethod]
        public void StopUnregistersInstanceFromWebHostingEnvironmentOnceAllAsyncMethodsAreCompleted()
        {
            IRegisteredObject unregisteredObject = null;
            StubHostingEnvironment.OnUnregisterObject = obj => unregisteredObject = obj;

            IRegisteredObject objectUnregisteredWhileRunningAsyncMethods = null;
            Func<Task> asyncMethod = () =>
            {
                objectUnregisteredWhileRunningAsyncMethods = unregisteredObject;
                return TaskEx.FromResult<object>(null);
            };

            var service = new TestableWebApplicationLifecycle(typeof(StubHostingEnvironment));
            service.Stopping += (sender, args) => args.Run(asyncMethod);

            service.Stop(false);

            Assert.IsNull(objectUnregisteredWhileRunningAsyncMethods);
            Assert.Same(service, unregisteredObject);
        }

        [TestMethod]
        public void StopUnregistersInstanceFromWebHostingEnvironmentDuringImmediateShutdown()
        {
            IRegisteredObject unregisteredObject = null;
            StubHostingEnvironment.OnUnregisterObject = obj => unregisteredObject = obj;

            var service = new TestableWebApplicationLifecycle(typeof(StubHostingEnvironment));
            service.Stop(true);

            Assert.Same(service, unregisteredObject);
        }

        private static class StubHostingEnvironment
        {
            public static Action<IRegisteredObject> OnRegisterObject = obj => { };
            public static Action<IRegisteredObject> OnUnregisterObject = obj => { };

            public static void RegisterObject(IRegisteredObject obj)
            {
                OnRegisterObject(obj);
            }

            public static void UnregisterObject(IRegisteredObject obj)
            {
                OnUnregisterObject(obj);
            }
        }

        private class TestableWebApplicationLifecycle : WebApplicationLifecycle
        {
            public TestableWebApplicationLifecycle(Type hostingEnvironment) : base(hostingEnvironment)
            {
            }
        }
    }
}
