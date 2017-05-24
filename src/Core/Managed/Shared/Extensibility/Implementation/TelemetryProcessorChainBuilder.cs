namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents an object used to Build a TelemetryProcessorChain.
    /// </summary>
    public sealed class TelemetryProcessorChainBuilder
    {
        private readonly List<Func<ITelemetryProcessor, ITelemetryProcessor>> factories;
        private readonly TelemetryConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryProcessorChainBuilder" /> class.
        /// </summary>
        /// <param name="configuration"> The <see cref="TelemetryConfiguration"/> instance to which the constructed processing chain should be set to. </param>        
        public TelemetryProcessorChainBuilder(TelemetryConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            this.configuration = configuration;
            this.factories = new List<Func<ITelemetryProcessor, ITelemetryProcessor>>();
        }

        /// <summary>
        /// Uses given factory to add TelemetryProcessor to the chain of processors. The processors
        /// in the chain will be invoked in the same order in which they are added.
        /// </summary>
        /// <param name="telemetryProcessorFactory">A delegate that returns a <see cref="ITelemetryProcessor"/>
        /// , given the next <see cref="ITelemetryProcessor"/> in the call chain.</param>
        public TelemetryProcessorChainBuilder Use(Func<ITelemetryProcessor, ITelemetryProcessor> telemetryProcessorFactory)
        {
            this.factories.Add(telemetryProcessorFactory);
            return this;
        }

        /// <summary>
        /// Uses given factory to add TelemetryProcessor at the start of the chain of processors. The processors
        /// in the chain will be invoked in the same order in which they are chained.
        /// </summary>
        /// <param name="telemetryProcessorFactory">A delegate that returns a <see cref="ITelemetryProcessor"/>
        /// , given the next <see cref="ITelemetryProcessor"/> in the call chain.</param>
        public TelemetryProcessorChainBuilder UseFirst(Func<ITelemetryProcessor, ITelemetryProcessor> telemetryProcessorFactory)
        {
            this.factories.Insert(0, telemetryProcessorFactory);
            return this;
        }

        /// <summary>
        /// Builds the chain of linked <see cref="ITelemetryProcessor" /> instances and sets the same in configuration object passed.
        /// A special telemetry processor for handling Transmission is always appended as the last
        /// processor in the chain.
        /// </summary>        
        public void Build()
        {
            var telemetryProcessorsList = new List<ITelemetryProcessor>();

            // TransmissionProcessor is always appended by default to the end of the chain.            
            ITelemetryProcessor linkedTelemetryProcessor = new TransmissionProcessor(this.configuration);
            telemetryProcessorsList.Add(linkedTelemetryProcessor);

            foreach (var generator in this.factories.AsEnumerable().Reverse())
            {
                ITelemetryProcessor prevTelemetryProcessor = linkedTelemetryProcessor;
                linkedTelemetryProcessor = generator.Invoke(linkedTelemetryProcessor);

                if (linkedTelemetryProcessor == null)
                {
                    // Loading of a telemetry processor failed, so skip it
                    // Error is logged when telemetry processor loading failed
                    linkedTelemetryProcessor = prevTelemetryProcessor;
                    continue;
                }

                telemetryProcessorsList.Add(linkedTelemetryProcessor);
            }

            var telemetryProcessorChain = new TelemetryProcessorChain(telemetryProcessorsList.AsEnumerable().Reverse());
            this.configuration.TelemetryProcessorChain = telemetryProcessorChain;
        }
    }
}
