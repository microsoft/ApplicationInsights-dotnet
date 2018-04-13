namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Telemetry type used to track exceptions.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=723596">Learn more</a>
    /// </summary>
    public sealed class ExceptionTelemetry : ITelemetry, ISupportProperties, ISupportSampling, ISupportMetrics
    {
        internal const string TelemetryName = "Exception";
        internal readonly string BaseType = typeof(ExceptionData).Name;
        internal readonly ExceptionData Data;

        private readonly TelemetryContext context;
        private Exception exception;
        private string message;

        private double? samplingPercentage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTelemetry"/> class with empty properties.
        /// </summary>
        public ExceptionTelemetry()
        {
            this.Data = new ExceptionData();
            this.context = new TelemetryContext(this.Data.properties);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTelemetry"/> class with empty properties.
        /// </summary>
        /// <param name="exception">Exception instance.</param>
        public ExceptionTelemetry(Exception exception)
            : this()
        {
            if (exception == null)
            {
                exception = new Exception(Utils.PopulateRequiredStringValue(null, "message", typeof(ExceptionTelemetry).FullName));
            }

            this.Exception = exception;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTelemetry"/> class by cloning an existing instance.
        /// </summary>
        /// <param name="source">Source instance of <see cref="ExceptionTelemetry"/> to clone from.</param>
        private ExceptionTelemetry(ExceptionTelemetry source)
        {
            this.Data = source.Data.DeepClone();
            this.context = source.context.DeepClone(this.Data.properties);
            this.Sequence = source.Sequence;
            this.Timestamp = source.Timestamp;
            this.samplingPercentage = source.samplingPercentage;
            this.Exception = source.Exception;
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
                return this.Data.problemId;
            }

            set
            {
                this.Data.problemId = value;
            }
        }

        /// <summary>
        /// Gets or sets the value indicated where the exception was handled.
        /// </summary>
        [Obsolete("Use custom properties to report exception handling layer")]
        public ExceptionHandledAt HandledAt
        {
            get
            {
                ExceptionHandledAt result = default(ExceptionHandledAt);
                if (this.Properties.ContainsKey("handledAt"))
                {
                    Enum.TryParse<ExceptionHandledAt>(this.Properties["handledAt"], out result);
                }

                return result;
            }

            set
            {
                this.Properties["handledAt"] = value.ToString();
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
                this.UpdateExceptions(value);
            }
        }

        /// <summary>
        /// Gets or sets ExceptionTelemetry message.
        /// </summary>
        public string Message
        {
            get
            {
                return this.message;
            }

            set
            {
                this.message = value;

                if (this.Data.exceptions != null && this.Data.exceptions.Count > 0)
                {
                    this.Data.exceptions[0].message = value;
                }
                else
                {
                    this.UpdateExceptions(this.Exception);
                }
            }
        }

        /// <summary>
        /// Gets a dictionary of application-defined exception metrics.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, double> Metrics
        {
            get { return this.Data.measurements; }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this exception.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get { return this.Data.properties; }
        }

        /// <summary>
        /// Gets or sets Exception severity level.
        /// </summary>
        public SeverityLevel? SeverityLevel
        {
            get { return this.Data.severityLevel.TranslateSeverityLevel(); }
            set { this.Data.severityLevel = value.TranslateSeverityLevel(); }
        }

        /// <summary>
        /// Gets or sets data sampling percentage (between 0 and 100).
        /// Should be 100/n where n is an integer. <a href="https://go.microsoft.com/fwlink/?linkid=832969">Learn more</a>
        /// </summary>
        double? ISupportSampling.SamplingPercentage
        {
            get { return this.samplingPercentage; }
            set { this.samplingPercentage = value; }
        }

        internal IList<ExceptionDetails> Exceptions
        {
            get { return this.Data.exceptions; }
        }

        /// <summary>
        /// Deeply clones a <see cref="ExceptionTelemetry"/> object.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        public ITelemetry DeepClone()
        {
            return new ExceptionTelemetry(this);
        }

        /// <summary>
        /// Set parsedStack from an array of StackFrame objects.
        /// </summary>
        public void SetParsedStack(System.Diagnostics.StackFrame[] frames)
        {
            List<StackFrame> orderedStackTrace = new List<StackFrame>();

            if (this.Exceptions != null && this.Exceptions.Count > 0)
            {
                if (frames != null && frames.Length > 0)
                {
                    int stackLength = 0;

                    this.Exceptions[0].parsedStack = new List<StackFrame>();
                    this.Exceptions[0].hasFullStack = true;

                    for (int level = 0; level < frames.Length; level++)
                    {
                        StackFrame sf = ExceptionConverter.GetStackFrame(frames[level], level);

                        stackLength += ExceptionConverter.GetStackFrameLength(sf);

                        if (stackLength > ExceptionConverter.MaxParsedStackLength)
                        {
                            this.Exceptions[0].hasFullStack = false;
                            break;
                        }

                        this.Exceptions[0].parsedStack.Add(sf);
                    }
                }
            }
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            // Sanitize on the ExceptionDetails stack information for raw stack and parsed stack is done while creating the object in ExceptionConverter.cs
            this.Properties.SanitizeProperties();
            this.Metrics.SanitizeMeasurements();
        }

        private void ConvertExceptionTree(Exception exception, ExceptionDetails parentExceptionDetails, List<ExceptionDetails> exceptions)
        {
            if (exception == null)
            {
                exception = new Exception(Utils.PopulateRequiredStringValue(null, "message", typeof(ExceptionTelemetry).FullName));
            }

            ExceptionDetails exceptionDetails = ExceptionConverter.ConvertToExceptionDetails(exception, parentExceptionDetails);

            // For upper level exception see if Message was provided and do not use exceptiom.message in that case
            if (parentExceptionDetails == null && !string.IsNullOrWhiteSpace(this.Message))
            {
                exceptionDetails.message = this.Message;
            }

            exceptions.Add(exceptionDetails);

            AggregateException aggregate = exception as AggregateException;
            if (aggregate != null)
            {
                foreach (Exception inner in aggregate.InnerExceptions)
                {
                    this.ConvertExceptionTree(inner, exceptionDetails, exceptions);
                }
            }
            else if (exception.InnerException != null)
            {
                this.ConvertExceptionTree(exception.InnerException, exceptionDetails, exceptions);
            }
        }

        private void UpdateExceptions(Exception exception)
        {
            // collect the set of exceptions detail info from the passed in exception
            List<ExceptionDetails> exceptions = new List<ExceptionDetails>();
            this.ConvertExceptionTree(exception, null, exceptions);

            // trim if we have too many, also add a custom exception to let the user know we're trimmed
            if (exceptions.Count > Constants.MaxExceptionCountToSave)
            {
                // TODO: when we localize these messages, we should consider not using InvariantCulture
                // create our "message" exception.
                InnerExceptionCountExceededException countExceededException = new InnerExceptionCountExceededException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The number of inner exceptions was {0} which is larger than {1}, the maximum number allowed during transmission. All but the first {1} have been dropped.",
                        exceptions.Count,
                        Constants.MaxExceptionCountToSave));

                // remove all but the first N exceptions
                exceptions.RemoveRange(Constants.MaxExceptionCountToSave, exceptions.Count - Constants.MaxExceptionCountToSave);

                // we'll add our new exception and parent it to the root exception (first one in the list)
                exceptions.Add(ExceptionConverter.ConvertToExceptionDetails(countExceededException, exceptions[0]));
            }

            this.Data.exceptions = exceptions;
        }
    }
}
