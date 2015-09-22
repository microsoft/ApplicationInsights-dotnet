namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Represents an object used to Build a composite TelemetryChannel.
    /// </summary>
    public sealed class TelemetryChannelBuilder
    {
        private List<Func<ITelemetryProcessor, ITelemetryProcessor>> factories;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryChannelBuilder" /> class.
        /// </summary>
        public TelemetryChannelBuilder()
        {
            this.factories = new List<Func<ITelemetryProcessor, ITelemetryProcessor>>();
        }

        /// <summary>
        /// Uses given factory to add TelemetryProcessor to the composed TelemetryChannel.
        /// </summary>
        /// <param name="telemetryProcessorFactory">A delegate that returns a <see cref="ITelemetryProcessor"/>
        /// , given the next <see cref="ITelemetryProcessor"/> in the call chain.</param>
        public TelemetryChannelBuilder Use(Func<ITelemetryProcessor, ITelemetryProcessor> telemetryProcessorFactory)
        {
            this.factories.Add(telemetryProcessorFactory);
            return this;
        }

        /// <summary>
        /// Builds a composite TelemetryChannel.
        /// </summary>
        /// <returns><see cref="ITelemetryChannel"/> object.</returns>
        public ITelemetryChannel Build()
        {
            var serverTelemetryChannel = new ServerTelemetryChannel();
            ITelemetryProcessor linkedTelemetryProcessor = serverTelemetryChannel.TelemetryProcessor;
            foreach (var generator in this.factories.AsEnumerable().Reverse())
            {
                linkedTelemetryProcessor = generator.Invoke(linkedTelemetryProcessor);
                if (linkedTelemetryProcessor == null)
                {
                    throw new InvalidOperationException("TelemetryProcessor returned from TelemetryProcessorFactory cannot be null.");
                }
            }

            serverTelemetryChannel.TelemetryProcessor = linkedTelemetryProcessor;
            return serverTelemetryChannel;
        }
    }
}
