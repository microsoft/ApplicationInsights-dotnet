namespace Microsoft.ApplicationInsights.TestFramework
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// A stub of <see cref="IContextInitializer"/>.
    /// </summary>
    public sealed class StubContextInitializer : IContextInitializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubContextInitializer"/> class.
        /// </summary>
        public StubContextInitializer()
        {
            this.OnInitialize = context => { };
        }

        /// <summary>
        /// Gets or sets the callback invoked by the <see cref="Initialize"/> method.
        /// </summary>
        public TelemetryContextAction OnInitialize { get; set; }

        /// <summary>
        /// Implements the <see cref="IContextInitializer.Initialize"/> method by invoking the <see cref="OnInitialize"/> callback.
        /// </summary>
        public void Initialize(TelemetryContext context)
        {
            this.OnInitialize(context);
        }
    }
}
