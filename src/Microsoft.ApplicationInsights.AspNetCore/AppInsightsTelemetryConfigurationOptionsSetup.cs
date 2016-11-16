using System.Collections.Generic;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
#if NET451
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
#endif
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class AppInsightsTelemetryConfigurationOptionsSetup : ConfigureOptions<ApplicationInsightsServiceOptions>
    {
        public AppInsightsTelemetryConfigurationOptionsSetup(
            ITelemetryChannel telemetryChannel,
            IEnumerable<ITelemetryInitializer> initializers,
            IEnumerable<ITelemetryModule> modules) : base(options => Configure(options, telemetryChannel, initializers, modules))
        {
        }

        private static void Configure(ApplicationInsightsServiceOptions options,
            ITelemetryChannel telemetryChannel,
            IEnumerable<ITelemetryInitializer> initializers,
            IEnumerable<ITelemetryModule> modules)
        {
            options.TelemetryConfiguration = TelemetryConfiguration.Active;
            AddTelemetryChannelAndProcessorsForFullFramework(options);
            options.TelemetryConfiguration.TelemetryChannel = telemetryChannel ?? options.TelemetryConfiguration.TelemetryChannel;

            foreach (var telemetryInitializer in initializers)
            {
                options.TelemetryConfiguration.TelemetryInitializers.Add(telemetryInitializer);
            }

            foreach (var telemetryModule in modules)
            {
                telemetryModule.Initialize(options.TelemetryConfiguration);
            }
        }

        private static void AddTelemetryChannelAndProcessorsForFullFramework(ApplicationInsightsServiceOptions options)
        {
#if NET451
            var configuration = options.TelemetryConfiguration;

            // Adding Server Telemetry Channel if services doesn't have an existing channel
            configuration.TelemetryChannel = configuration.TelemetryChannel ?? new ServerTelemetryChannel();

            if (configuration.TelemetryChannel is ServerTelemetryChannel)
            {
                // Enabling Quick Pulse Metric Stream
                if (options.EnableQuickPulseMetricStream)
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
                    if (options.EnableAdaptiveSampling)
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