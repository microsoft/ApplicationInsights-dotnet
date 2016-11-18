namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
#if NET451
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
#endif
    using Microsoft.Extensions.Options;

    internal class TelemetryConfigurationOptionsSetup : IConfigureOptions<TelemetryConfiguration>
    {
        private readonly ApplicationInsightsServiceOptions applicationInsightsServiceOptions;
        private readonly IEnumerable<ITelemetryInitializer> initializers;
        private readonly IEnumerable<ITelemetryModule> modules;
        private readonly ITelemetryChannel telemetryChannel;

        public TelemetryConfigurationOptionsSetup(
            IServiceProvider serviceProvider,
            IOptions<ApplicationInsightsServiceOptions> applicationInsightsServiceOptions,
            IEnumerable<ITelemetryInitializer> initializers,
            IEnumerable<ITelemetryModule> modules)
        {
            this.applicationInsightsServiceOptions = applicationInsightsServiceOptions.Value;
            this.initializers = initializers;
            this.modules = modules;
            this.telemetryChannel = serviceProvider.GetService<ITelemetryChannel>();
        }

        public void Configure(TelemetryConfiguration options)
        {
            if (this.applicationInsightsServiceOptions.InstrumentationKey != null)
            {
                options.InstrumentationKey = this.applicationInsightsServiceOptions.InstrumentationKey;
            }

            this.AddTelemetryChannelAndProcessorsForFullFramework(options);

            options.TelemetryChannel = this.telemetryChannel ?? options.TelemetryChannel;

            if (this.applicationInsightsServiceOptions.DeveloperMode != null)
            {
                options.TelemetryChannel.DeveloperMode = this.applicationInsightsServiceOptions.DeveloperMode;
            }

            if (this.applicationInsightsServiceOptions.EndpointAddress != null)
            {
                options.TelemetryChannel.EndpointAddress = this.applicationInsightsServiceOptions.EndpointAddress;
            }

            foreach (var initializer in this.initializers)
            {
                options.TelemetryInitializers.Add(initializer);
            }

            foreach (var module in this.modules)
            {
                module.Initialize(options);
            }
        }

        private void AddTelemetryChannelAndProcessorsForFullFramework(TelemetryConfiguration configuration)
        {
#if NET451

            // Adding Server Telemetry Channel if services doesn't have an existing channel
            configuration.TelemetryChannel = this.telemetryChannel ?? new ServerTelemetryChannel();
            if (configuration.TelemetryChannel is ServerTelemetryChannel)
            {
                // Enabling Quick Pulse Metric Stream
                if (this.applicationInsightsServiceOptions.EnableQuickPulseMetricStream)
                {
                    var quickPulseModule = new QuickPulseTelemetryModule();
                    quickPulseModule.Initialize(configuration);

                    QuickPulseTelemetryProcessor processor = null;
                    configuration.TelemetryProcessorChainBuilder.Use((next) => {
                        processor = new QuickPulseTelemetryProcessor(next);
                        quickPulseModule.RegisterTelemetryProcessor(processor);
                        return processor;
                    });
                }

                // Enabling Adaptive Sampling and initializing server telemetry channel with configuration
                if (configuration.TelemetryChannel.GetType() == typeof(ServerTelemetryChannel))
                {
                    if (this.applicationInsightsServiceOptions.EnableAdaptiveSampling)
                    {
                        configuration.TelemetryProcessorChainBuilder.UseAdaptiveSampling();
                    }
                    (configuration.TelemetryChannel as ServerTelemetryChannel).Initialize(configuration);
                }

                configuration.TelemetryProcessorChainBuilder.Build();
            }
#endif
        }
    }
}