namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.W3C;
    using Microsoft.Extensions.Options;
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Initializes TelemetryConfiguration based on values in <see cref="ApplicationInsightsServiceOptions"/>
    /// and registered <see cref="ITelemetryInitializer"/>s and <see cref="ITelemetryModule"/>s.
    /// </summary>
    internal class TelemetryConfigurationOptionsSetup : IConfigureOptions<TelemetryConfiguration>
    {
        private readonly ApplicationInsightsServiceOptions applicationInsightsServiceOptions;
        private readonly IEnumerable<ITelemetryInitializer> initializers;
        private readonly IEnumerable<ITelemetryModule> modules;
        private readonly ITelemetryChannel telemetryChannel;
        private readonly IEnumerable<ITelemetryProcessorFactory> telemetryProcessorFactories;
        private readonly IEnumerable<ITelemetryModuleConfigurator> telemetryModuleConfigurators;
        private readonly IApplicationIdProvider applicationIdProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryConfigurationOptionsSetup"/> class.
        /// </summary>
        public TelemetryConfigurationOptionsSetup(
            IServiceProvider serviceProvider,
            IOptions<ApplicationInsightsServiceOptions> applicationInsightsServiceOptions,
            IEnumerable<ITelemetryInitializer> initializers,
            IEnumerable<ITelemetryModule> modules,
            IEnumerable<ITelemetryProcessorFactory> telemetryProcessorFactories,
            IEnumerable<ITelemetryModuleConfigurator> telemetryModuleConfigurators)
        {
            this.applicationInsightsServiceOptions = applicationInsightsServiceOptions.Value;
            this.initializers = initializers;
            this.modules = modules;
            this.telemetryProcessorFactories = telemetryProcessorFactories;
            this.telemetryModuleConfigurators = telemetryModuleConfigurators;
            this.telemetryChannel = serviceProvider.GetService<ITelemetryChannel>();
            this.applicationIdProvider = serviceProvider.GetService<IApplicationIdProvider>();
        }

        /// <inheritdoc />
        public void Configure(TelemetryConfiguration configuration)
        {
            try
            {
                if (this.applicationInsightsServiceOptions.InstrumentationKey != null)
                {
                    configuration.InstrumentationKey = this.applicationInsightsServiceOptions.InstrumentationKey;
                }

                if (this.telemetryModuleConfigurators.Any())
                {
                    foreach (ITelemetryModuleConfigurator telemetryModuleConfigurator in this.telemetryModuleConfigurators)
                    {
                        ITelemetryModule telemetryModule = this.modules.FirstOrDefault((module) => module.GetType() == telemetryModuleConfigurator.TelemetryModuleType);
                        if (telemetryModule != null)
                        {
                            telemetryModuleConfigurator.Configure(telemetryModule, this.applicationInsightsServiceOptions);
                        }
                        else
                        {
                            AspNetCoreEventSource.Instance.UnableToFindModuleToConfigure(telemetryModuleConfigurator.TelemetryModuleType.ToString());
                        }
                    }
                }

                if (this.telemetryProcessorFactories.Any())
                {
                    foreach (ITelemetryProcessorFactory processorFactory in this.telemetryProcessorFactories)
                    {
                        configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Use(processorFactory.Create);
                    }
                }

                // Fallback to default channel (InMemoryChannel) created by base sdk if no channel is found in DI
                configuration.TelemetryChannel = this.telemetryChannel ?? configuration.TelemetryChannel;
                (configuration.TelemetryChannel as ITelemetryModule)?.Initialize(configuration);

                this.AddAutoCollectedMetricExtractor(configuration);
                this.AddQuickPulse(configuration);
                this.AddSampling(configuration);
                this.DisableHeartBeatIfConfigured();
                
                configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Build();
                configuration.TelemetryProcessorChainBuilder.Build();

                if (this.applicationInsightsServiceOptions.DeveloperMode != null)
                {
                    configuration.TelemetryChannel.DeveloperMode = this.applicationInsightsServiceOptions.DeveloperMode;
                }

                if (this.applicationInsightsServiceOptions.EndpointAddress != null)
                {
                    configuration.TelemetryChannel.EndpointAddress = this.applicationInsightsServiceOptions.EndpointAddress;
                }

                foreach (ITelemetryInitializer initializer in this.initializers)
                {
                    configuration.TelemetryInitializers.Add(initializer);
                }

                foreach (ITelemetryModule module in this.modules)
                {
                    module.Initialize(configuration);
                }

                foreach (ITelemetryProcessor processor in configuration.TelemetryProcessors)
                {
                    ITelemetryModule module = processor as ITelemetryModule;
                    if (module != null)
                    {
                        module.Initialize(configuration);
                    }
                }

                // Microsoft.ApplicationInsights.DependencyCollector.DependencyTrackingTelemetryModule depends on this nullable configuration to support Correlation.
                configuration.ApplicationIdProvider = this.applicationIdProvider;
            }
            catch (Exception ex)
            {
                AspNetCoreEventSource.Instance.TelemetryConfigurationSetupFailure(ex.ToInvariantString());
            }
        }

        private void AddQuickPulse(TelemetryConfiguration configuration)
        {
            if (this.applicationInsightsServiceOptions.EnableQuickPulseMetricStream)
            {
                QuickPulseTelemetryModule quickPulseModule = this.modules.FirstOrDefault((module) => module.GetType() == typeof(QuickPulseTelemetryModule)) as QuickPulseTelemetryModule;
                if (quickPulseModule != null)
                {
                    QuickPulseTelemetryProcessor processor = null;
                    configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Use((next) =>
                    {
                        processor = new QuickPulseTelemetryProcessor(next);
                        quickPulseModule.RegisterTelemetryProcessor(processor);
                        return processor;
                    });
                }
                else
                {
                    AspNetCoreEventSource.Instance.UnableToFindQuickPulseModuleInDI();
                }
            }
        }

        private void AddSampling(TelemetryConfiguration configuration)
        {
            if (this.applicationInsightsServiceOptions.EnableAdaptiveSampling)
            {
                AdaptiveSamplingPercentageEvaluatedCallback samplingCallback = (ratePerSecond, currentPercentage, newPercentage, isChanged, estimatorSettings) =>
                {
                    if (isChanged)
                    {
                        configuration.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Request, newPercentage);
                    }
                };

                SamplingPercentageEstimatorSettings settings = new SamplingPercentageEstimatorSettings();
                settings.MaxTelemetryItemsPerSecond = 5;
                configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder.UseAdaptiveSampling(settings, samplingCallback, excludedTypes: "Event");
                configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder.UseAdaptiveSampling(5, includedTypes: "Event");
            }
        }

        private void AddAutoCollectedMetricExtractor(TelemetryConfiguration configuration)
        {
            if (this.applicationInsightsServiceOptions.AddAutoCollectedMetricExtractor)
            {
                configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder.Use(next => new AutocollectedMetricsExtractor(next));
            }
        }

        private void DisableHeartBeatIfConfigured()
        {
            // Disable heartbeat if user sets it (by default it is on)
            if (!this.applicationInsightsServiceOptions.EnableHeartbeat)
            {
                foreach (var module in TelemetryModules.Instance.Modules)
                {
                    if (module is IHeartbeatPropertyManager hbeatMan)
                    {
                        hbeatMan.IsHeartbeatEnabled = false;
                    }
                }
            }
        }
    }
}