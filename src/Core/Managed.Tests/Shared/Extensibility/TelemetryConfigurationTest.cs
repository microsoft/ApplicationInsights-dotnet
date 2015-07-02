namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.ComponentModel;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
#if WINRT
    using TaskEx = System.Threading.Tasks.Task;
#endif

    // TODO: Add Dispose tests to TelemetryConfigurationTest.
    [TestClass]
    public class TelemetryConfigurationTest
    {
        [TestMethod]
        public void TelemetryConfigurationIsPublicToAllowUsersManipulateConfigurationProgrammatically()
        {
            Assert.True(typeof(TelemetryConfiguration).GetTypeInfo().IsPublic);
        }

        #region Active

        [TestMethod]
        public void ActiveIsPublicToAllowUsersToAccessActiveTelemetryConfigurationInAdvancedScenarios()
        {
#if NET35
            Assert.True(typeof(TelemetryConfiguration).GetTypeInfo().GetDeclaredProperty("Active").GetGetMethod().IsPublic);
#else
            Assert.True(typeof(TelemetryConfiguration).GetTypeInfo().GetDeclaredProperty("Active").GetMethod.IsPublic);
#endif
        }

        [TestMethod]
        public void ActiveSetterIsInternalAndNotMeantToBeUsedByOurCustomers()
        {
#if NET35
            Assert.False(typeof(TelemetryConfiguration).GetTypeInfo().GetDeclaredProperty("Active").GetSetMethod(true).IsPublic);
#else
            Assert.False(typeof(TelemetryConfiguration).GetTypeInfo().GetDeclaredProperty("Active").SetMethod.IsPublic);
#endif
        }

        [TestMethod]
        public void ActiveIsLazilyInitializedToDelayCostOfLoadingConfigurationFromFile()
        {
            try
            {
                TelemetryConfiguration.Active = null;
                Assert.NotNull(TelemetryConfiguration.Active);
            }
            finally
            {
                TelemetryConfiguration.Active = null;
            }
        }

        [TestMethod]
        public void ActiveUsesTelemetryConfigurationFactoryToInitializeTheInstance()
        {
            bool factoryInvoked = false;
            TelemetryConfigurationFactory.Instance = new StubTelemetryConfigurationFactory
            {
                OnInitialize = configuration => { factoryInvoked = true; },
            };
            TelemetryConfiguration.Active = null;
            try
            {
                var dummy = TelemetryConfiguration.Active;
                Assert.True(factoryInvoked);
            }
            finally
            {
                TelemetryConfigurationFactory.Instance = null;
                TelemetryConfiguration.Active = null;
            }
        }

        [TestMethod]
        public void ActiveInitializesSingleInstanceRegardlessOfNumberOfThreadsTryingToAccessIt()
        {
            int numberOfInstancesInitialized = 0;
            TelemetryConfiguration.Active = null;
            TelemetryConfigurationFactory.Instance = new StubTelemetryConfigurationFactory
            {
                OnInitialize = configuration => { Interlocked.Increment(ref numberOfInstancesInitialized); },
            };
            try
            {
                var tasks = new Task[8];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = TaskEx.Run(() => TelemetryConfiguration.Active);
                }

                Task.WaitAll(tasks);
                Assert.Equal(1, numberOfInstancesInitialized);
            }
            finally
            {
                TelemetryConfiguration.Active = null;
                TelemetryConfigurationFactory.Instance = null;
            }
        }

        [TestMethod]
        [Timeout(1000)]
        public void ActiveInitializesSingleInstanceWhenConfigurationComponentsAccessActiveRecursively()
        {
            int numberOfInstancesInitialized = 0;
            TelemetryConfiguration.Active = null;
            TelemetryConfigurationFactory.Instance = new StubTelemetryConfigurationFactory
            {
                OnInitialize = configuration =>
                {
                    Interlocked.Increment(ref numberOfInstancesInitialized);
                    var dummy = TelemetryConfiguration.Active;
                },
            };
            try
            {
                var dummy = TelemetryConfiguration.Active;
                Assert.Equal(1, numberOfInstancesInitialized);
            }
            finally
            {
                TelemetryConfiguration.Active = null;
                TelemetryConfigurationFactory.Instance = null;
            }
        }

        #endregion

        #region CreateDefault

        [TestMethod]
        public void CreateDefaultReturnsNewConfigurationInstanceInitializedByTelemetryConfigurationFactory()
        {
            TelemetryConfiguration initializedConfiguration = null;
            TelemetryConfigurationFactory.Instance = new StubTelemetryConfigurationFactory
            {
                OnInitialize = configuration => initializedConfiguration = configuration,
            };
            try
            {
                var defaultConfiguration = TelemetryConfiguration.CreateDefault();
                Assert.NotNull(defaultConfiguration);
                Assert.Same(defaultConfiguration, initializedConfiguration);
            }
            finally
            {
                TelemetryConfigurationFactory.Instance = null;
            }
        }

        #endregion

        [TestMethod]
        public void DisableTelemetryIsFalseByDefault()
        {
            var configuration = new TelemetryConfiguration();

            Assert.False(configuration.DisableTelemetry);
        }

        #region InstrumentationKey

        [TestMethod]
        public void InstrumentationKeyIsEmptyStringByDefaultToAvoidNullReferenceExceptionWhenAccessingPropertyValue()
        {
            var configuration = new TelemetryConfiguration();
            Assert.Equal(0, configuration.InstrumentationKey.Length);
        }

        [TestMethod]
        public void InstrumentationKeyThrowsArgumentNullExceptionWhenNewValueIsNullToAvoidNullReferenceExceptionWhenAccessingPropertyValue()
        {
            var configuration = new TelemetryConfiguration();
            Xunit.Assert.Throws<ArgumentNullException>(() => configuration.InstrumentationKey = null);
        }

        [TestMethod]
        public void InstrumentationKeyCanBeSetToProgrammaticallyDefineInstrumentationKeyForAllContextsInApplication()
        {
            var configuration = new TelemetryConfiguration();
            configuration.InstrumentationKey = "99C6A712-B2B5-46E3-97F4-F83F69999324";
            Assert.Equal("99C6A712-B2B5-46E3-97F4-F83F69999324", configuration.InstrumentationKey);
        }

        #endregion

        #region SamplingPercentage

        [TestMethod]
        public void SamplingPercentageIs100ByDefault()
        {
            var configuration = new TelemetryConfiguration();
            Assert.Equal(100, configuration.SamplingPercentage);
        }

        [TestMethod]
        public void SamplingPercentageCanBeSetToProgrammaticallyDefineSamplingPercentageForAllContextsInApplication()
        {
            var configuration = new TelemetryConfiguration();
            configuration.SamplingPercentage = 50;
            Assert.Equal(50, configuration.SamplingPercentage);
        }

        #endregion

        #region ContextInitializers

        [TestMethod]
        public void ContextInitializersReturnsAnEmptyListByDefaultToAvoidNullReferenceExceptionsInUserCode()
        {
            var configuration = new TelemetryConfiguration();
            Assert.Equal(0, configuration.ContextInitializers.Count);
        }

        [TestMethod]
        public void ContextInitializersReturnsThreadSafeList()
        {
            var configuration = new TelemetryConfiguration();
            Assert.Equal(typeof(SnapshottingList<IContextInitializer>), configuration.ContextInitializers.GetType());
        }

        #endregion

        #region TelemetryInitializers

        [TestMethod]
        public void TelemetryInitializersReturnsAnEmptyListByDefaultToAvoidNullReferenceExceptionsInUserCode()
        {
            var configuration = new TelemetryConfiguration();
            Assert.Equal(0, configuration.TelemetryInitializers.Count);
        }

        [TestMethod]
        public void TelemetryInitializersReturnsThreadSafeList()
        {
            var configuration = new TelemetryConfiguration();
            Assert.Equal(typeof(SnapshottingList<ITelemetryInitializer>), configuration.TelemetryInitializers.GetType());
        }

        #endregion

        #region TelemetryChannel

        [TestMethod]
        public void TelemetryChannelIsNullByDefaultToAvoidLockEscalation()
        {
            var configuration = new TelemetryConfiguration();
            Assert.Null(configuration.TelemetryChannel);
        }

        [TestMethod]
        public void TelemetryChannelCanBeSetByUserToReplaceDefaultChannelForTesting()
        {
            var configuration = new TelemetryConfiguration();

            var customChannel = new StubTelemetryChannel();
            configuration.TelemetryChannel = customChannel;

            Assert.Same(customChannel, configuration.TelemetryChannel);
        }

        #endregion

        private class StubTelemetryConfigurationFactory : TelemetryConfigurationFactory
        {
            public Action<TelemetryConfiguration> OnInitialize = configuration => { };

            public override void Initialize(TelemetryConfiguration configuration)
            {
                this.OnInitialize(configuration);
            }
        }
    }
}
