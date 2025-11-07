namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

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

        private readonly bool isCreatedFromExceptionInfo = false;

        private TelemetryContext context;
        private Exception exception;
        private string message;
        private double? samplingPercentage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTelemetry"/> class with empty properties.
        /// </summary>
        public ExceptionTelemetry()
        {
            // keeping this.Data init so that getters don't throw an exception when this.Data is null.
            this.Data = new ExceptionInfo(new List<ExceptionDetailsInfo>(), null, null,
                 new Dictionary<string, string>());
            this.context = new TelemetryContext();
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
        /// Initializes a new instance of the <see cref="ExceptionTelemetry"/> class.
        /// </summary>
        /// <param name="exceptionDetailsInfoList">Exception info.</param>
        /// <param name="severityLevel">Severity level.</param>
        /// <param name="problemId">Problem id.</param>
        /// <param name="properties">Properties.</param>
        public ExceptionTelemetry(IEnumerable<ExceptionDetailsInfo> exceptionDetailsInfoList, SeverityLevel? severityLevel, string problemId,
            IDictionary<string, string> properties)
        {
            this.isCreatedFromExceptionInfo = true;

            ExceptionInfo exceptionInfo = new ExceptionInfo(exceptionDetailsInfoList, severityLevel, problemId, properties);

            this.Data = exceptionInfo;
            this.context = new TelemetryContext(this.Data.Properties);

            // this.UpdateData(exceptionInfo);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTelemetry"/> class by cloning an existing instance.
        /// </summary>
        /// <param name="source">Source instance of <see cref="ExceptionTelemetry"/> to clone from.</param>
        private ExceptionTelemetry(ExceptionTelemetry source)
        {
            this.isCreatedFromExceptionInfo = source.isCreatedFromExceptionInfo;

            this.context = source.context.DeepClone(this.Data.Properties);
            this.Sequence = source.Sequence;
            this.Timestamp = source.Timestamp;
            this.samplingPercentage = source.samplingPercentage;

            if (!this.isCreatedFromExceptionInfo)
            {
                this.exception = source.Exception;
            }
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
                return /*this.isCreatedFromExceptionInfo
                    ? this.ConstructExceptionFromDetailsInfo(this.Data.ExceptionDetailsInfoList ?? new List<ExceptionDetailsInfo>().AsReadOnly())
                    :*/ this.exception;
            }

            set
            {
                if (this.isCreatedFromExceptionInfo)
                {
                    throw new InvalidOperationException(
                        "The property is unavailable to be set on an instance created with the ExceptionDetailsInfo-based constructor");
                }

                this.exception = value;
                // this.UpdateData(value);
            }
        }

        /// <summary>
        /// Gets or sets ExceptionTelemetry message.
        /// </summary>
        public string Message
        {
            get
            {
                const string ExceptionMessageSeparator = " <--- ";

                return this.isCreatedFromExceptionInfo
                    ? (this.Data.ExceptionDetailsInfoList != null ? string.Join(ExceptionMessageSeparator, this.Data.ExceptionDetailsInfoList.Select(info => info.Message)) : string.Empty)
                    : this.message;
            }

            set
            {
                if (this.isCreatedFromExceptionInfo)
                {
                    throw new InvalidOperationException(
                        "The property is unavailable to be set on an instance created with the ExceptionDetailsInfo-based constructor");
                }

                this.message = value;

                if (this.Data.ExceptionDetailsInfoList != null && this.Data.ExceptionDetailsInfoList.Count > 0)
                {
                    this.Data.ExceptionDetailsInfoList[0].Message = value;
                }
                else
                {
                    // this.UpdateData(this.Exception);
                }
            }
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
            get;
        }

        /// <summary>
        /// Gets or sets Exception severity level.
        /// </summary>
        public SeverityLevel? SeverityLevel
        {
            get => this.Data.SeverityLevel;
            set => this.Data.SeverityLevel = value;
        }

        /*internal IList<ExceptionDetails> Exceptions
        {
            get { return this.Data.Data.exceptions; }
        }*/

        /// <summary>
        /// Set parsedStack from an array of StackFrame objects.
        /// </summary>
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1801 // Review unused parameters
        public void SetParsedStack(System.Diagnostics.StackFrame[] frames)
#pragma warning restore CA1801 // Review unused parameters
#pragma warning restore CA1822 // Mark members as static
        {
        }

        /*
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

            if (exception is AggregateException aggregate)
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

        private void UpdateData(Exception exception)
        {
            if (this.isCreatedFromExceptionInfo)
            {
                throw new InvalidOperationException("Operation is not supported given the state of the object.");
            }

            try
            {
                // collect the set of exceptions detail info from the passed in exception
                List<ExceptionDetails> exceptions = new List<ExceptionDetails>();
                this.ConvertExceptionTree(exception, null, exceptions);

                // trim if we have too many, also add a custom exception to let the user know we're trimmed
                if (exceptions.Count > Constants.MaxExceptionCountToSave)
                {
                    // TODO: when we localize these messages, we should consider not using InvariantCulture
                    // create our "message" exception.
                    InnerExceptionCountExceededException countExceededException =
                        new InnerExceptionCountExceededException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "The number of inner exceptions was {0} which is larger than {1}, the maximum number allowed during transmission. All but the first {1} have been dropped.",
                                exceptions.Count,
                                Constants.MaxExceptionCountToSave));

                    // remove all but the first N exceptions
                    exceptions.RemoveRange(Constants.MaxExceptionCountToSave,
                        exceptions.Count - Constants.MaxExceptionCountToSave);

                    // we'll add our new exception and parent it to the root exception (first one in the list)
                    exceptions.Add(ExceptionConverter.ConvertToExceptionDetails(countExceededException, exceptions[0]));
                }

                this.Data = new ExceptionInfo(exceptions.Select(ex => new ExceptionDetailsInfo(ex)), this.SeverityLevel,
                    this.ProblemId, this.Properties);

                if (this.context == null)
                {
                    this.context = new TelemetryContext(this.Data.Properties);
                }
                else
                {
                    this.context = this.context.DeepClone(this.Data.Properties);
                }
            }
            catch (Exception ex)
            {
                CoreEventSource.Log.UpdateDataFailed(ex.ToInvariantString());
            }
        }

        private void UpdateData(ExceptionInfo exceptionInfo)
        {
            if (!this.isCreatedFromExceptionInfo)
            {
                throw new InvalidOperationException("Operation is not supported given the state of the object.");
            }

            this.Data = exceptionInfo ?? throw new ArgumentNullException(nameof(exceptionInfo));
            this.context = new TelemetryContext(this.Data.Properties);
        }

        private Exception ConstructExceptionFromDetailsInfo(IReadOnlyList<ExceptionDetailsInfo> exceptionInfos)
        {
            if (!this.isCreatedFromExceptionInfo)
            {
                throw new InvalidOperationException("Operation is not supported given the state of the object.");
            }

            // construct a fake Exception object based on provided information
            if (!exceptionInfos.Any())
            {
                return new Exception(string.Empty);
            }

            return new Exception(exceptionInfos[0].Message, this.ConstructInnerException(exceptionInfos, 0));
        }

        private Exception ConstructInnerException(IReadOnlyList<ExceptionDetailsInfo> exceptionInfos, int parentExceptionIndex)
        {
            // inner exception is the next one after the parent
            int index = parentExceptionIndex + 1;

            if (index < exceptionInfos.Count)
            {
                // inner exception exists
                return new Exception(exceptionInfos[index].Message, this.ConstructInnerException(exceptionInfos, index));
            }

            // inner exception doesn't exist
            return null;
        }*/
    }
}
