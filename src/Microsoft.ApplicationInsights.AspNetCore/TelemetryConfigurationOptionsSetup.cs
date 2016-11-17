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

namespace Microsoft.Extensions.DependencyInjection
{
    public class TelemetryConfigurationOptionsSetup : IConfigureOptions<TelemetryConfiguration>
    {
        private readonly ApplicationInsightsServiceOptions _applicationInsightsServiceOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<ITelemetryInitializer> _initializers;
        private readonly IEnumerable<ITelemetryModule> _modules;
        private readonly ITelemetryChannel _telemetryChannel;

        public TelemetryConfigurationOptionsSetup(
            IServiceProvider serviceProvider,
            IOptions<ApplicationInsightsServiceOptions> applicationInsightsServiceOptions,
            IEnumerable<ITelemetryInitializer> initializers,
            IEnumerable<ITelemetryModule> modules)
        {
            _applicationInsightsServiceOptions = applicationInsightsServiceOptions.Value;
            _serviceProvider = serviceProvider;
            _initializers = initializers;
            _modules = modules;
            _telemetryChannel = _serviceProvider.GetService<ITelemetryChannel>();
        }

        public void Configure(TelemetryConfiguration options)
        {
            if (_applicationInsightsServiceOptions.InstrumentationKey != null)
            {
                options.InstrumentationKey = _applicationInsightsServiceOptions.InstrumentationKey;
            }

            AddTelemetryChannelAndProcessorsForFullFramework(options);

            options.TelemetryChannel = _telemetryChannel ?? options.TelemetryChannel;

            if (_applicationInsightsServiceOptions.DeveloperMode != null)
            {
                options.TelemetryChannel.DeveloperMode = _applicationInsightsServiceOptions.DeveloperMode;
            }
            if (_applicationInsightsServiceOptions.EndpointAddress != null)
            {
                options.TelemetryChannel.EndpointAddress = _applicationInsightsServiceOptions.EndpointAddress;
            }
            foreach (var initializer in _initializers)
            {
                options.TelemetryInitializers.Add(initializer);
            }

            foreach (var module in _modules)
            {
                module.Initialize(options);
            }
        }

        private void AddTelemetryChannelAndProcessorsForFullFramework(TelemetryConfiguration configuration)
        {
#if NET451

            // Adding Server Telemetry Channel if services doesn't have an existing channel
            configuration.TelemetryChannel = _telemetryChannel ?? new ServerTelemetryChannel();
            if (configuration.TelemetryChannel is ServerTelemetryChannel)
            {
                // Enabling Quick Pulse Metric Stream
                if (_applicationInsightsServiceOptions.EnableQuickPulseMetricStream)
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
                    if (_applicationInsightsServiceOptions.EnableAdaptiveSampling)
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