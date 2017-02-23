namespace Microsoft.ApplicationInsights.Extensibility.Metrics
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.ObjectModel;

    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class TelemetryConfigurationTest
    {
        [TestMethod]
        public void TelemetryConfigurationIsPublicToAllowUsersManipulateConfigurationProgrammatically()
        {
            Assert.True(typeof(TelemetryConfiguration).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void NewTelemetryConfigurationWithChannelUsesSpecifiedChannel()
        {
            StubTelemetryChannel stubChannel = new StubTelemetryChannel();
            TelemetryConfiguration config = new TelemetryConfiguration(string.Empty, stubChannel);
            Assert.Same(stubChannel, config.TelemetryChannel);
            FieldInfo shouldDisposeChannelField = typeof(TelemetryConfiguration).GetField("shouldDisposeChannel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            Assert.False((bool)shouldDisposeChannelField.GetValue(config));
        }

        [TestMethod]
        public void NewTelemetryConfigurationWithoutChannelCreatesDefaultInMemoryChannel()
        {
            TelemetryConfiguration config = new TelemetryConfiguration();
            Assert.NotNull(config.TelemetryChannel);
            Assert.Equal(typeof(Channel.InMemoryChannel), config.TelemetryChannel.GetType());
            FieldInfo shouldDisposeChannelField = typeof(TelemetryConfiguration).GetField("shouldDisposeChannel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            Assert.True((bool)shouldDisposeChannelField.GetValue(config));
        }

        [TestMethod]
        public void NewTelemetryConfigurationWithInstrumentationKeyAndChannelUsesSpecifiedKeyAndChannel()
        {
            string expectedKey = "expected";
            StubTelemetryChannel stubChannel = new StubTelemetryChannel();
            TelemetryConfiguration config = new TelemetryConfiguration(expectedKey, stubChannel);
            Assert.Equal(expectedKey, config.InstrumentationKey);
            Assert.Same(stubChannel, config.TelemetryChannel);
            FieldInfo shouldDisposeChannelField = typeof(TelemetryConfiguration).GetField("shouldDisposeChannel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            Assert.False((bool)shouldDisposeChannelField.GetValue(config));
        }

        [TestMethod]
        public void NewTelemetryConfigurationWithInstrumentationKeyButNoChannelCreatesDefaultInMemoryChannel()
        {
            string expectedKey = "expected";
            TelemetryConfiguration config = new TelemetryConfiguration(expectedKey);
            Assert.Equal(expectedKey, config.InstrumentationKey);
            Assert.NotNull(config.TelemetryChannel);
            Assert.Equal(typeof(Channel.InMemoryChannel), config.TelemetryChannel.GetType());
            FieldInfo shouldDisposeChannelField = typeof(TelemetryConfiguration).GetField("shouldDisposeChannel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            Assert.True((bool)shouldDisposeChannelField.GetValue(config));
        }

        #region Active

        [TestMethod]
        public void ActiveIsPublicToAllowUsersToAccessActiveTelemetryConfigurationInAdvancedScenarios()
        {
            Assert.True(typeof(TelemetryConfiguration).GetTypeInfo().GetDeclaredProperty("Active").GetMethod.IsPublic);
        }

        [TestMethod]
        public void ActiveSetterIsInternalAndNotMeantToBeUsedByOurCustomers()
        {
            Assert.False(typeof(TelemetryConfiguration).GetTypeInfo().GetDeclaredProperty("Active").SetMethod.IsPublic);
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
            Assert.NotNull(TelemetryConfiguration.Active);

            Assert.Same(modules, TelemetryModules.Instance);
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
                OnInitialize = (configuration, _) => { Interlocked.Increment(ref numberOfInstancesInitialized); },
            };
            try
            {
                var tasks = new Task[8];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Run(() => TelemetryConfiguration.Active);
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
                OnInitialize = (configuration, _) =>
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

            Assert.NotNull(TelemetryConfiguration.CreateDefault());
            Assert.Null(modules);
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
        public void TelemetryChannelCanBeSetByUserToReplaceDefaultChannelForTesting()
        {
            var configuration = new TelemetryConfiguration();

            var customChannel = new StubTelemetryChannel();
            configuration.TelemetryChannel = customChannel;

            Assert.Same(customChannel, configuration.TelemetryChannel);
        }

        #endregion

        #region TelemetryProcessor

        [TestMethod]
        public void TelemetryConfigurationAlwaysGetDefaultTransmissionProcessor()
        {
            var configuration = new TelemetryConfiguration();
            var tp = configuration.TelemetryProcessorChain;

            Assert.IsType<TransmissionProcessor>(tp.FirstTelemetryProcessor);
        }

        [TestMethod]
        public void TelemetryProcessorsCollectionIsReadOnly()
        {
            var configuration = new TelemetryConfiguration();
            
            Assert.IsType<ReadOnlyCollection<ITelemetryProcessor>>(configuration.TelemetryProcessors);
        }

        #endregion

        #region MetricProcessors

        [TestMethod]
        public void MetricProcessorsReturnsAnEmptyListByDefaultToAvoidNullReferenceExceptionsInUserCode()
        {
            var configuration = new TelemetryConfiguration();
            Assert.Equal(0, configuration.MetricProcessors.Count);
        }

        [TestMethod]
        public void MetricPrcessorsReturnsThreadSafeList()
        {
            var configuration = new TelemetryConfiguration();
            Assert.Equal(typeof(SnapshottingList<IMetricProcessor>), configuration.MetricProcessors.GetType());
        }

        #endregion

        #region Serialized Configuration
        [TestMethod]
        public void TelemetryConfigThrowsIfSerializedConfigIsNull()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
             {
                 TelemetryConfiguration.CreateFromConfiguration(null);
             });
        }

        [TestMethod]
        public void TelemetryConfigThrowsIfSerializedConfigIsEmpty()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                TelemetryConfiguration.CreateFromConfiguration(String.Empty);
            });
        }

        [TestMethod]
        public void TelemetryConfigThrowsIfSerializedConfigIsWhitespace()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
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
