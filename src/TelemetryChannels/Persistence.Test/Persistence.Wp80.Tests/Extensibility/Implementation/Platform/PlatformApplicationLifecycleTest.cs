namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO.IsolatedStorage;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.Phone.Shell;
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using Assert = Xunit.Assert;

    /// <summary>
    /// Windows Phone (Silverlight runtime) tests for the <see cref="PlatformApplicationLifecycle"/> class.
    /// </summary>
    [TestClass]
    public partial class PlatformApplicationLifecycleTest
    {
        [TestMethod]
        public void ApplicationStartedEventIsRaisedWhenNewInstanceOfPhoneAppIsLaunched()
        {
            dynamic phoneApplicationService = CreateFakePhoneApplicationService();

            var applicationLifecycle = new PlatformApplicationLifecycle
                                           {
                                               Dispatcher = CreateFakeDispatcher(),
                                               GetPhoneApplicationService = () => phoneApplicationService
                                           };
            applicationLifecycle.Initialize();

            object applicationStartedEventArgs = null;
            applicationLifecycle.Started += (sender, args) =>
            {
                applicationStartedEventArgs = args;
            };

            var launchingEventArgs = new LaunchingEventArgs();
            phoneApplicationService.Launching(phoneApplicationService, launchingEventArgs);

            Assert.Same(launchingEventArgs, applicationStartedEventArgs);
        }

        [TestMethod]
        public void ApplicationStartedEventIsRaisedWhenExistingInstanceOfPhoneAppIsActivated()
        {
            dynamic phoneApplicationService = CreateFakePhoneApplicationService();

            var applicationLifecycle = new PlatformApplicationLifecycle
                                           {
                                               Dispatcher = CreateFakeDispatcher(),
                                               GetPhoneApplicationService = () => phoneApplicationService
                                           };
            applicationLifecycle.Initialize();

            object applicationStartedEventArgs = null;
            applicationLifecycle.Started += (sender, args) =>
            {
                applicationStartedEventArgs = args;
            };

            var activatedEventArgs = new ActivatedEventArgs();
            phoneApplicationService.Activated(phoneApplicationService, activatedEventArgs);

            Assert.Same(activatedEventArgs, applicationStartedEventArgs);            
        }

        [TestMethod]
        public void ApplicationStoppingEventIsRaisedWhenPhoneAppIsDeactivated()
        {
            dynamic phoneApplicationService = CreateFakePhoneApplicationService();

            var applicationLifecycle = new PlatformApplicationLifecycle
                                            {
                                                Dispatcher = CreateFakeDispatcher(),
                                                GetPhoneApplicationService = () => phoneApplicationService
                                            };
            applicationLifecycle.Initialize();

            object applicationStoppingSender = null;
            object applicationStoppingEventArgs = null;
            applicationLifecycle.Stopping += (sender, args) =>
            {
                applicationStoppingSender = sender;
                applicationStoppingEventArgs = args;
            };

            var deactivatedEventArgs = new DeactivatedEventArgs();
            phoneApplicationService.Deactivated(phoneApplicationService, deactivatedEventArgs);

            Assert.NotNull(applicationStoppingSender);
            Assert.NotNull(applicationStoppingEventArgs);            
        }

        [TestMethod]
        public void ApplicationStoppingEventArgsRunsAsyncTasksWithCurrentThreadTaskSchedulerToPreventSuspendingOfAsyncOperations()
        {
            dynamic phoneApplicationService = CreateFakePhoneApplicationService();

            var applicationLifecycle = new PlatformApplicationLifecycle
                                            {
                                                Dispatcher = CreateFakeDispatcher(),
                                                GetPhoneApplicationService = () => phoneApplicationService
                                            };

            applicationLifecycle.Initialize();

            TaskScheduler actualTaskScheduler = null;
            applicationLifecycle.Stopping += (sender, args) => args.Run(() => Task.Factory.StartNew(() => actualTaskScheduler = TaskScheduler.Current));

            var deactivatedEventArgs = new DeactivatedEventArgs();
            phoneApplicationService.Deactivated(phoneApplicationService, deactivatedEventArgs);

            Assert.IsType(typeof(CurrentThreadTaskScheduler), actualTaskScheduler);
        }
        
        [TestMethod]
        public void ApplicationStoppingEventIsRaisedWhenPhoneAppIsClosing()
        {
            dynamic phoneApplicationService = CreateFakePhoneApplicationService();

            var applicationLifecycle = new PlatformApplicationLifecycle
                                            {
                                                Dispatcher = CreateFakeDispatcher(),
                                                GetPhoneApplicationService = () => phoneApplicationService
                                            };
            applicationLifecycle.Initialize();

            object applicationStoppingSender = null;
            object applicationStoppingEventArgs = null;
            applicationLifecycle.Stopping += (sender, args) =>
            {
                applicationStoppingSender = sender;
                applicationStoppingEventArgs = args;
            };

            var closingEventArgs = new ClosingEventArgs();
            phoneApplicationService.Closing(phoneApplicationService, closingEventArgs);

            Assert.NotNull(applicationStoppingSender);
            Assert.NotNull(applicationStoppingEventArgs);
        }

        [TestMethod]
        public void ConstructorInvokesInitializeOnDispatcherThreadToLetApplicationConstructPhoneApplicationServiceFromXaml()
        {
            Action actionInvokedOnDispatcherThread = null;
            var dispatcher = new StubPlatformDispatcher
            {
                OnRunAsync = action =>
                {
                    actionInvokedOnDispatcherThread = action;
                    return Task.FromResult<object>(null);
                },
            };

            var applicationLifecycle = new PlatformApplicationLifecycle
            {
                Dispatcher = dispatcher,
                GetPhoneApplicationService = CreateFakePhoneApplicationService
            };
            applicationLifecycle.Initialize();
            
            Assert.NotNull(actionInvokedOnDispatcherThread);
        }

        [TestMethod]
        public void InitializeThrowsInvalidOperationExceptionWhenPhoneApplicationServiceIsNotAvailable()
        {
            var applicationLifecycle = new PlatformApplicationLifecycle
            {
                Dispatcher = CreateFakeDispatcher(),
                GetPhoneApplicationService = () => null
            };

            var exception = Assert.Throws<InvalidOperationException>(() => applicationLifecycle.Initialize());
            Assert.Contains(typeof(PhoneApplicationService).Name, exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void OnApplicationStartedDoesNotThrowWhithoutEventHandlers()
        {
            dynamic phoneApplicationService = CreateFakePhoneApplicationService();

            var applicationLifecycle = new PlatformApplicationLifecycle
                                            {
                                                Dispatcher = CreateFakeDispatcher(),
                                                GetPhoneApplicationService = () => phoneApplicationService
                                            };
            applicationLifecycle.Initialize();

            phoneApplicationService.Activated(phoneApplicationService, new ActivatedEventArgs());
        }

        [TestMethod]
        public void OnApplicationStoppingDoesNotThrowWhithoutEventHandlers()
        {
            dynamic phoneApplicationService = CreateFakePhoneApplicationService();
            var applicationLifecycle = new PlatformApplicationLifecycle
                                            {
                                                Dispatcher = CreateFakeDispatcher(),
                                                GetPhoneApplicationService = () => phoneApplicationService
                                            };
            applicationLifecycle.Initialize();

            phoneApplicationService.Deactivated(phoneApplicationService, new DeactivatedEventArgs());
        }

        private static IPlatformDispatcher CreateFakeDispatcher()
        {
            return new StubPlatformDispatcher
            {
                OnRunAsync = action =>
                {
                    action();
                    return Task.FromResult<object>(null);
                },
            };
        }

        private static dynamic CreateFakePhoneApplicationService()
        {
            dynamic fake = new ExpandoObject();
            fake.Activated = new EventHandler<ActivatedEventArgs>(delegate { });
            fake.Closing = new EventHandler<ClosingEventArgs>(delegate { });
            fake.Deactivated = new EventHandler<DeactivatedEventArgs>(delegate { });
            fake.Launching = new EventHandler<LaunchingEventArgs>(delegate { });
            return fake;
        }
    }
}
