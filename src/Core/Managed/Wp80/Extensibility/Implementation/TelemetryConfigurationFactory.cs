namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;

    internal class TelemetryConfigurationFactory
    {
        private const string InstrumentationKeyOpeingTag = "<InstrumentationKey>";
        private const string InstrumentationKeyClosingTag = "</InstrumentationKey>";

        private static TelemetryConfigurationFactory instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryConfigurationFactory"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is protected because <see cref="TelemetryConfigurationFactory"/> is only meant to be instantiated 
        /// by the <see cref="Instance"/> property or by tests.
        /// </remarks>
        protected TelemetryConfigurationFactory()
        {
        }

        /// <summary>
        /// Gets or sets the default <see cref="TelemetryConfigurationFactory"/> instance used by <see cref="TelemetryConfiguration"/>.
        /// </summary>
        /// <remarks>
        /// This property is a test isolation "pinch point" that allows us to test <see cref="TelemetryConfiguration"/> without using reflection.
        /// </remarks>
        public static TelemetryConfigurationFactory Instance
        {
            get { return instance ?? (instance = new TelemetryConfigurationFactory()); }
            set { instance = value; }
        }

        public virtual void Initialize(TelemetryConfiguration configuration)
        {
            configuration.TelemetryInitializers.Add(new SdkVersionPropertyTelemetryInitializer());
            configuration.TelemetryInitializers.Add(new TimestampPropertyInitializer());            

            // Load customizations from the ApplicationsInsights.config file
            string text = PlatformSingleton.Current.ReadConfigurationXml();
            string instrumentationKey = this.GetInstrumentationKeyFromConfigFile(text);
            if (!string.IsNullOrEmpty(instrumentationKey))
            {
                configuration.InstrumentationKey = instrumentationKey;
            }

            // Creating the default channel if no channel configuration supplied
            configuration.TelemetryChannel = configuration.TelemetryChannel ?? new InMemoryChannel();

            // Creating the the processor chain with default processor (transmissionprocessor) if none configured            
            if (configuration.TelemetryProcessors == null)
            {
                configuration.GetTelemetryProcessorChainBuilder().Build();
            }
        }

        private string GetInstrumentationKeyFromConfigFile(string text)
        {
            string instrumentationKey = string.Empty;
            text = text.Replace(" ", string.Empty);

            // Calculating the start index of the instrumentation key
            int index = text.IndexOf(InstrumentationKeyOpeingTag, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                int startLocation = index + InstrumentationKeyOpeingTag.Length;

                // Calculating the end of the instrumentation key
                int endLocation = text.IndexOf(InstrumentationKeyClosingTag, startLocation, StringComparison.OrdinalIgnoreCase);
                if (endLocation > 0)
                {
                    instrumentationKey = text.Substring(startLocation, endLocation - startLocation);
                }
            }

            return instrumentationKey;
        }
    }
}
