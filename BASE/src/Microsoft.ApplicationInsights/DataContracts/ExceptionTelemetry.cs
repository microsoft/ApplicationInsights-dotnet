namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Telemetry type used to track exceptions. This will capture TypeName, Message, and CallStack.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=723596">Learn more</a>
    /// </summary>
    /// <remarks>
    /// Additional exception details will need to be tracked manually.
    /// </remarks>
    public sealed class ExceptionTelemetry : ITelemetry, ISupportProperties
    {
        internal const string EtwEnvelopeName = "Exception";
        internal string EnvelopeName = "AppExceptions";

        internal ExceptionInfo Data = null;

        private TelemetryContext context;
        private Exception exception;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTelemetry"/> class with empty properties.
        /// </summary>
        public ExceptionTelemetry()
        {
            this.Data = new ExceptionInfo(new List<ExceptionDetailsInfo>(), null, null, new Dictionary<string, string>());
            this.context = new TelemetryContext();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTelemetry"/> class with empty properties.
        /// </summary>
        /// <param name="exception">Exception instance.</param>
        public ExceptionTelemetry(Exception exception)
            : this()
        {
            this.Exception = exception;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTelemetry"/> class.
        /// </summary>
        /// <param name="exceptionDetailsInfoList">Exception info.</param>
        /// <param name="severityLevel">Severity level.</param>
        /// <param name="problemId">Problem id.</param>
        /// <param name="properties">Properties.</param>
        public ExceptionTelemetry(IEnumerable<ExceptionDetailsInfo> exceptionDetailsInfoList, SeverityLevel? severityLevel, string problemId,
            IDictionary<string, string> properties)
        {
            this.Data = new ExceptionInfo(exceptionDetailsInfoList, severityLevel, problemId, properties);
            this.context = new TelemetryContext(this.Data.Properties);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTelemetry"/> class by cloning an existing instance.
        /// </summary>
        /// <param name="source">Source instance of <see cref="ExceptionTelemetry"/> to clone from.</param>
        private ExceptionTelemetry(ExceptionTelemetry source)
        {
            this.Data = new ExceptionInfo(source.Data.ExceptionDetailsInfoList, source.Data.SeverityLevel, source.Data.ProblemId, source.Data.Properties);
            this.context = source.context.DeepClone(this.Data.Properties);
            this.Sequence = source.Sequence;
            this.Timestamp = source.Timestamp;
            this.exception = source.exception;
        }

        /// <summary>
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public string Sequence { get; set; }

        /// <summary>
        /// Gets the context associated with the current telemetry item.
        /// </summary>
        public TelemetryContext Context
        {
            get { return this.context; }
        }

        /// <summary>
        /// Gets or sets the problemId.
        /// </summary>
        public string ProblemId
        {
            get
            {
                return this.Data.ProblemId;
            }

            set
            {
                this.Data.ProblemId = value;
            }
        }

        /// <summary>
        /// Gets or sets the original exception tracked by this <see cref="ITelemetry"/>.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return this.exception;
            }

            set
            {
                this.exception = value;
            }
        }

        /// <summary>
        /// Gets or sets ExceptionTelemetry message.
        /// </summary>
        public string Message
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of <see cref="ExceptionDetailsInfo"/>. User can modify the contents of individual object, but
        /// not the list itself.
        /// </summary>
        public IReadOnlyList<ExceptionDetailsInfo> ExceptionDetailsInfoList => this.Data.ExceptionDetailsInfoList;

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this exception.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, string> Properties
        {
#pragma warning disable CS0618 // Type or member is obsolete
            // TODO: Remove context and Add a private ConcurrentDictionary<string,string>.
            get
            {
                if (this.context == null)
                {
                    this.context = new TelemetryContext();
                }

                return this.context.Properties;
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Gets or sets Exception severity level.
        /// </summary>
        public SeverityLevel? SeverityLevel
        {
            get => this.Data.SeverityLevel;
            set => this.Data.SeverityLevel = value;
        }

        /// <summary>
        /// Deeply clones a <see cref="ExceptionTelemetry"/> object.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        public ITelemetry DeepClone()
        {
            return new ExceptionTelemetry(this);
        }
    }
}
