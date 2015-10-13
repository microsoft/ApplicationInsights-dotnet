namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Encapsulates the global telemetry configuration typically loaded from the ApplicationInsights.config file.
    /// </summary>
    /// <remarks>
    /// All <see cref="TelemetryContext"/> objects are initialized using the <see cref="Active"/> 
    /// telemetry configuration provided by this class.
    /// </remarks>
    public sealed class TelemetryConfiguration : IDisposable
    {
        private static object syncRoot = new object();
        private static TelemetryConfiguration active;

        private readonly SnapshottingList<IContextInitializer> contextInitializers = new SnapshottingList<IContextInitializer>();
        private readonly SnapshottingList<ITelemetryInitializer> telemetryInitializers = new SnapshottingList<ITelemetryInitializer>();
        private TelemetryProcessorChain telemetryProcessorChain;
        private string instrumentationKey = string.Empty;
        private bool disableTelemetry = false;

        /// <summary>
        /// Gets the active <see cref="TelemetryConfiguration"/> instance loaded from the ApplicationInsights.config file. 
        /// If the configuration file does not exist, the active configuration instance is initialized with minimum defaults 
        /// needed to send telemetry to Application Insights.
        /// </summary>
        public static TelemetryConfiguration Active
        {
            get 
            { 
                if (active == null)
                {
                    lock (syncRoot)
                    {
                        if (active == null)
                        {
                            active = new TelemetryConfiguration();
                            TelemetryConfigurationFactory.Instance.Initialize(active);
                        }
                    }
                }

                return active;
            }

            internal set 
            {
                lock (syncRoot)
                {
                    active = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the default instrumentation key for the application.
        /// </summary>
        /// <exception cref="ArgumentNullException">The new value is null.</exception>
        /// <remarks>
        /// This instrumentation key value is used by default by all <see cref="TelemetryClient"/> instances
        /// created in the application. This value can be overwritten by setting the <see cref="TelemetryContext.InstrumentationKey"/>
        /// property of the <see cref="TelemetryClient.Context"/>.
        /// </remarks>
        public string InstrumentationKey
        {
            get
            {
                return this.instrumentationKey;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.instrumentationKey = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether sending of telemetry to Application Insights is disabled.
        /// </summary>
        /// <remarks>
        /// This disable tracking setting value is used by default by all <see cref="TelemetryClient"/> instances
        /// created in the application. 
        /// </remarks>
        public bool DisableTelemetry
        {
            get
            {
                return this.disableTelemetry;
            }

            set
            {
                // Log the state of tracking 
                if (value)
                {
                    CoreEventSource.Log.TrackingWasDisabled();
                }
                else
                {
                    CoreEventSource.Log.TrackingWasEnabled();
                }

                this.disableTelemetry = value;
            }
        }

        /// <summary>
        /// Gets the list of <see cref="IContextInitializer"/> objects that supply additional information about application.
        /// </summary>
        /// <remarks>
        /// Context initializers extend Application Insights telemetry collection by supplying additional information 
        /// about application environment, such as <see cref="TelemetryContext.User"/> or <see cref="TelemetryContext.Device"/> 
        /// information that remains constant during application lifetime. A <see cref="TelemetryClient"/> invokes context 
        /// initializers to obtain initial property values for <see cref="TelemetryContext"/> object during its construction.
        /// The default list of context initializers is provided by the Application Insights NuGet packages and loaded from 
        /// the ApplicationInsights.config file located in the application directory. 
        /// </remarks>
        public IList<IContextInitializer> ContextInitializers
        {
            get { return this.contextInitializers; }
        }

        /// <summary>
        /// Gets the list of <see cref="ITelemetryInitializer"/> objects that supply additional information about telemetry.
        /// </summary>
        /// <remarks>
        /// Telemetry initializers extend Application Insights telemetry collection by supplying additional information 
        /// about individual <see cref="ITelemetry"/> items, such as <see cref="ITelemetry.Timestamp"/>. A <see cref="TelemetryClient"/>
        /// invokes telemetry initializers each time <see cref="TelemetryClient.Track"/> method is called.
        /// The default list of telemetry initializers is provided by the Application Insights NuGet packages and loaded from 
        /// the ApplicationInsights.config file located in the application directory. 
        /// </remarks>
        public IList<ITelemetryInitializer> TelemetryInitializers
        {
            get { return this.telemetryInitializers; }
        }

        /// <summary>
        /// Gets the first telemetry processor in the chain of processors.
        /// </summary>
        public TelemetryProcessorChain TelemetryProcessors
        {
            get
            {                
                if (this.telemetryProcessorChain == null)
                {                                                 
                    // Gets the telemetryprocessorchainbuilder, builds the processor chain and sets the same in the configuration.
                    this.GetTelemetryProcessorChainBuilder().Build();
                }

                return this.telemetryProcessorChain;
            }

            internal set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("TelemetryProcessorChain");
                }

                this.telemetryProcessorChain = value;
            }              
        }

        /// <summary>
        /// Gets or sets the telemetry channel.
        /// </summary>
        public ITelemetryChannel TelemetryChannel { get; set; }

        /// <summary>
        /// Creates a new <see cref="TelemetryConfiguration"/> instance loaded from the ApplicationInsights.config file.
        /// If the configuration file does not exist, the new configuration instance is initialized with minimum defaults 
        /// needed to send telemetry to Application Insights.
        /// </summary>
        public static TelemetryConfiguration CreateDefault()
        {
            var configuration = new TelemetryConfiguration();
            TelemetryConfigurationFactory.Instance.Initialize(configuration);

            return configuration;
        }

        /// <summary>
        /// Releases resources used by the current instance of the <see cref="TelemetryConfiguration"/> class.
        /// </summary>
        public void Dispose()
        {
            // TODO: Implement a Finalizer to dispose this class.
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Interlocked.CompareExchange(ref active, null, this);

                ITelemetryChannel telemetryChannel = this.TelemetryChannel;
                if (telemetryChannel != null)
                {
                    telemetryChannel.Dispose();
                }
            }
        }
    }
}