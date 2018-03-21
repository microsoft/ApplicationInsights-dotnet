namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.ObjectModel;

    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    

    using TaskEx = System.Threading.Tasks.Task;

    [TestClass]
    public class TelemetryConfigurationTest
    {
        [TestMethod]
        public void TelemetryConfigurationIsPublicToAllowUsersManipulateConfigurationProgrammatically()
        {
            Assert.IsTrue(typeof(TelemetryConfiguration).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void NewTelemetryConfigurationWithChannelUsesSpecifiedChannel()
        {
            StubTelemetryChannel stubChannel = new StubTelemetryChannel();
            bool channelDisposed = false;
            stubChannel.OnDispose += () => { channelDisposed = true; };
            TelemetryConfiguration config = new TelemetryConfiguration(string.Empty, stubChannel);
            Assert.AreSame(stubChannel, config.TelemetryChannel);
            config.Dispose();
            Assert.IsFalse(channelDisposed);
        }

        [TestMethod]
        public void NewTelemetryConfigurationWithoutChannelCreatesDefaultInMemoryChannel()
        {
            TelemetryConfiguration config = new TelemetryConfiguration();
            var channel = config.TelemetryChannel as Channel.InMemoryChannel;
            Assert.IsNotNull(channel);
            config.Dispose();
            Assert.IsTrue(channel.IsDisposed);
        }

        [TestMethod]
        public void NewTelemetryConfigurationWithInstrumentationKeyAndChannelUsesSpecifiedKeyAndChannel()
        {
            string expectedKey = "expected";
            StubTelemetryChannel stubChannel = new StubTelemetryChannel();
            bool channelDisposed = false;
            stubChannel.OnDispose += () => { channelDisposed = true; };
            TelemetryConfiguration config = new TelemetryConfiguration(expectedKey, stubChannel);
            Assert.AreEqual(expectedKey, config.InstrumentationKey);
            Assert.AreSame(stubChannel, config.TelemetryChannel);
            config.Dispose();
            Assert.IsFalse(channelDisposed);
        }

        [TestMethod]
        public void NewTelemetryConfigurationWithInstrumentationKeyButNoChannelCreatesDefaultInMemoryChannel()
        {
            string expectedKey = "expected";
            TelemetryConfiguration config = new TelemetryConfiguration(expectedKey);
            Assert.AreEqual(expectedKey, config.InstrumentationKey);
            var channel = config.TelemetryChannel as Channel.InMemoryChannel;
            Assert.IsNotNull(channel);
            config.Dispose();
            Assert.IsTrue(channel.IsDisposed);
        }

        #region Active

        [TestMethod]
        public void ActiveIsPublicToAllowUsersToAccessActiveTelemetryConfigurationInAdvancedScenarios()
        {
            Assert.IsTrue(typeof(TelemetryConfiguration).GetTypeInfo().GetDeclaredProperty("Active").GetGetMethod(true).IsPublic);
        }

        [TestMethod]
        public void ActiveSetterIsInternalAndNotMeantToBeUsedByOurCustomers()
        {
            Assert.IsFalse(typeof(TelemetryConfiguration).GetTypeInfo().GetDeclaredProperty("Active").GetSetMethod(true).IsPublic);
        }

        [TestMethod]
        public void ActiveIsLazilyInitializedToDelayCostOfLoadingConfigurationFromFile()
        {
            try
            {
                TelemetryConfiguration.Active = null;
                Assert.IsNotNull(TelemetryConfiguration.Active);
            }
            finally
            {
                TelemetryConfiguration.Active = null;
            }
        }

        [TestMethod]
        public void ActiveInitializesTelemetryModuleCollection()
        {
            TelemetryModules modules = new TestableTelemetryModules();
            TelemetryConfigurationFactory.Instance = new StubTelemetryConfigurationFactory
            {
                OnInitialize = (c, m) =>
                {
                    modules = m;
                },
            };

            TelemetryConfiguration.Active = null;
            Assert.IsNotNull(TelemetryConfiguration.Active);

            Assert.AreSame(modules, TelemetryModules.Instance);
        }

        [TestMethod]
        public void ActiveUsesTelemetryConfigurationFactoryToInitializeTheInstance()
        {
            bool factoryInvoked = false;
            TelemetryConfigurationFactory.Instance = new StubTelemetryConfigurationFactory
            {
                OnInitialize = (configuration, _) => { factoryInvoked = true; },
            };
            TelemetryConfiguration.Active = null;
            try
            {
                var dummy = TelemetryConfiguration.Active;
                Assert.IsTrue(factoryInvoked);
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
                OnInitialize = (configuration, _) => { Interlocked.Increment(ref numberOfInstancesInitialized); },
            };
            try
            {
                var tasks = new Task[8];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = TaskEx.Run(() => TelemetryConfiguration.Active);
                }

                Task.WaitAll(tasks);
                Assert.AreEqual(1, numberOfInstancesInitialized);
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
                OnInitialize = (configuration, _) =>
                {
                    Interlocked.Increment(ref numberOfInstancesInitialized);
                    var dummy = TelemetryConfiguration.Active;
                },
            };
            try
            {
                var dummy = TelemetryConfiguration.Active;
                Assert.AreEqual(1, numberOfInstancesInitialized);
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
        public void DefaultDoesNotInitializeTelemetryModuleCollection()
        {
            TelemetryModules modules = new TestableTelemetryModules();
            TelemetryConfigurationFactory.Instance = new StubTelemetryConfigurationFactory
            {
                OnInitialize = (c, m) =>
                {
                    modules = m;
                },
            };

            Assert.IsNotNull(TelemetryConfiguration.CreateDefault());
            Assert.IsNull(modules);
        }

        [TestMethod]
        public void CreateDefaultReturnsNewConfigurationInstanceInitializedByTelemetryConfigurationFactory()
        {
            TelemetryConfiguration initializedConfiguration = null;
            TelemetryConfigurationFactory.Instance = new StubTelemetryConfigurationFactory
            {
                OnInitialize = (configuration, _) => initializedConfiguration = configuration,
            };
            try
            {
                var defaultConfiguration = TelemetryConfiguration.CreateDefault();
                Assert.IsNotNull(defaultConfiguration);
                Assert.AreSame(defaultConfiguration, initializedConfiguration);
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

            Assert.IsFalse(configuration.DisableTelemetry);
        }

        #region InstrumentationKey

        [TestMethod]
        public void InstrumentationKeyIsEmptyStringByDefaultToAvoidNullReferenceExceptionWhenAccessingPropertyValue()
        {
            var configuration = new TelemetryConfiguration();
            Assert.AreEqual(0, configuration.InstrumentationKey.Length);
        }

        [TestMethod]
        public void InstrumentationKeyThrowsArgumentNullExceptionWhenNewValueIsNullToAvoidNullReferenceExceptionWhenAccessingPropertyValue()
        {
            var configuration = new TelemetryConfiguration();
            AssertEx.Throws<ArgumentNullException>(() => configuration.InstrumentationKey = null);
        }

        [TestMethod]
        public void InstrumentationKeyCanBeSetToProgrammaticallyDefineInstrumentationKeyForAllContextsInApplication()
        {
            var configuration = new TelemetryConfiguration();
            configuration.InstrumentationKey = "99C6A712-B2B5-46E3-97F4-F83F69999324";
            Assert.AreEqual("99C6A712-B2B5-46E3-97F4-F83F69999324", configuration.InstrumentationKey);
        }

        #endregion

        #region TelemetryInitializers

        [TestMethod]
        public void TelemetryInitializersReturnsAnEmptyListByDefaultToAvoidNullReferenceExceptionsInUserCode()
        {
            var configuration = new TelemetryConfiguration();
            Assert.AreEqual(0, configuration.TelemetryInitializers.Count);
        }

        [TestMethod]
        public void TelemetryInitializersReturnsThreadSafeList()
        {
            var configuration = new TelemetryConfiguration();
            Assert.AreEqual(typeof(SnapshottingList<ITelemetryInitializer>), configuration.TelemetryInitializers.GetType());
        }

        #endregion

        #region TelemetryChannel

        [TestMethod]
        public void TelemetryChannelCanBeSetByUserToReplaceDefaultChannelForTesting()
        {
            var configuration = new TelemetryConfiguration();

            var customChannel = new StubTelemetryChannel();
            configuration.TelemetryChannel = customChannel;

            Assert.AreSame(customChannel, configuration.TelemetryChannel);
        }

        [TestMethod]
        public void CommonTelemetryChannelIsDefaultSinkTelemetryChannel()
        {
            var configuration = new TelemetryConfiguration();

            var c1 = new StubTelemetryChannel();
            var c2 = new StubTelemetryChannel();

            configuration.TelemetryChannel = c1;
            Assert.AreSame(c1, configuration.TelemetryChannel);

            configuration.DefaultTelemetrySink.TelemetryChannel = c2;
            Assert.AreSame(c2, configuration.TelemetryChannel);
        }

        #endregion

        #region TelemetryProcessor

        [TestMethod]
        public void TelemetryConfigurationAlwaysGetDefaultTransmissionProcessor()
        {
            var configuration = new TelemetryConfiguration();
            var tp = configuration.DefaultTelemetrySink.TelemetryProcessorChain;

            AssertEx.IsType<TransmissionProcessor>(tp.FirstTelemetryProcessor);
        }

        [TestMethod]
        public void TelemetryProcessorsCollectionIsReadOnly()
        {
            var configuration = new TelemetryConfiguration();
            
            AssertEx.IsType<ReadOnlyCollection<ITelemetryProcessor>>(configuration.TelemetryProcessors);
        }

        #endregion

        #region MetricProcessors		

        [TestMethod]		
        public void MetricProcessorsReturnsAnEmptyListByDefaultToAvoidNullReferenceExceptionsInUserCode()
        {		
            var configuration = new TelemetryConfiguration();		
            Assert.AreEqual(0, configuration.MetricProcessors.Count);		
        }		
		
        [TestMethod]		
        public void MetricPrcessorsReturnsThreadSafeList()
        {		
            var configuration = new TelemetryConfiguration();		
            Assert.AreEqual(typeof(SnapshottingList<IMetricProcessorV1>), configuration.MetricProcessors.GetType());		
        }		
		
        #endregion

        #region Serialized Configuration
        [TestMethod]
        public void TelemetryConfigThrowsIfSerializedConfigIsNull()
        {
            AssertEx.Throws<ArgumentNullException>(() =>
             {
                 TelemetryConfiguration.CreateFromConfiguration(null);
             });
        }

        [TestMethod]
        public void TelemetryConfigThrowsIfSerializedConfigIsEmpty()
        {
            AssertEx.Throws<ArgumentNullException>(() =>
            {
                TelemetryConfiguration.CreateFromConfiguration(String.Empty);
            });
        }

        [TestMethod]
        public void TelemetryConfigThrowsIfSerializedConfigIsWhitespace()
        {
            AssertEx.Throws<ArgumentNullException>(() =>
            {
                TelemetryConfiguration.CreateFromConfiguration(" ");
            });
        }
        #endregion

        private class TestableTelemetryModules : TelemetryModules
        {
        }

        private class StubTelemetryConfigurationFactory : TelemetryConfigurationFactory
        {
            public Action<TelemetryConfiguration, TelemetryModules> OnInitialize = (configuration, module) => { };

            public override void Initialize(TelemetryConfiguration configuration, TelemetryModules modules)
            {
                this.OnInitialize(configuration, modules);
            }
        }
    }
}
