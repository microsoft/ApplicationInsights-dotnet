namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                if (this.exception == null && this.Data?.ExceptionDetailsInfoList != null && this.Data.ExceptionDetailsInfoList.Count > 0)
                {
                    // Lazy-load: reconstruct exception from ExceptionDetailsInfoList
                    this.exception = ReconstructExceptionFromDetails(this.Data.ExceptionDetailsInfoList);
                }

                return this.exception;
            }

            set
            {
                this.exception = value;
                
                // Populate ExceptionDetailsInfoList from the exception
                if (value != null)
                {
                    var exceptionDetailsList = new List<ExceptionDetailsInfo>();
                    ConvertExceptionTree(value, null, exceptionDetailsList);
                    this.Data = new ExceptionInfo(exceptionDetailsList, this.Data?.SeverityLevel, this.Data?.ProblemId, this.Data?.Properties ?? new Dictionary<string, string>());
                }
                else
                {
                    this.Data = new ExceptionInfo(new List<ExceptionDetailsInfo>(), this.Data?.SeverityLevel, this.Data?.ProblemId, this.Data?.Properties ?? new Dictionary<string, string>());
                }
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
            get
            {
                if (this.context == null)
                {
                    this.context = new TelemetryContext();
                }

                return this.context.Properties;
            }
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
        /// Set parsedStack from an array of StackFrame objects.
        /// </summary>
        /// <param name="frames">Array of System.Diagnostics.StackFrame to convert to parsed stack.</param>
        public void SetParsedStack(System.Diagnostics.StackFrame[] frames)
        {
            if (this.Data?.ExceptionDetailsInfoList != null && this.Data.ExceptionDetailsInfoList.Count > 0)
            {
                if (frames != null && frames.Length > 0)
                {
                    const int MaxParsedStackLength = 32768;
                    int stackLength = 0;

                    var parsedStack = new List<StackFrame>();
                    bool hasFullStack = true;

                    for (int level = 0; level < frames.Length; level++)
                    {
                        var frame = frames[level];
#pragma warning disable IL2026 // Suppressed for backward compatibility - method metadata might be incomplete when trimmed
                        var method = frame.GetMethod();
#pragma warning restore IL2026
                        
                        var sf = new StackFrame(
                            assembly: method?.DeclaringType?.Assembly?.FullName,
                            fileName: frame.GetFileName(),
                            level: level,
                            line: frame.GetFileLineNumber(),
                            method: method?.Name);

                        // Approximate stack frame length
                        stackLength += (sf.Assembly?.Length ?? 0) + (sf.FileName?.Length ?? 0) + (sf.Method?.Length ?? 0) + 20;

                        if (stackLength > MaxParsedStackLength)
                        {
                            hasFullStack = false;
                            break;
                        }

                        parsedStack.Add(sf);
                    }

                    // Update the first exception details with parsed stack
                    var firstException = this.Data.ExceptionDetailsInfoList[0] as ExceptionDetailsInfo;
                    if (firstException != null)
                    {
                        firstException.ParsedStack = parsedStack;
                        firstException.HasFullStack = hasFullStack;
                    }
                }
            }
        }

        /// <summary>
        /// Converts exception tree to a list of ExceptionDetailsInfo objects.
        /// </summary>
        private static void ConvertExceptionTree(Exception exception, int? parentId, List<ExceptionDetailsInfo> detailsList)
        {
            // Limit to prevent infinite loops
            if (exception == null || detailsList.Count >= 10)
            {
                return;
            }

            int currentId = detailsList.Count;
            var exceptionDetail = new ExceptionDetailsInfo(
                id: currentId,
                outerId: parentId ?? -1,
                typeName: exception.GetType().FullName,
                message: exception.Message,
                hasFullStack: exception.StackTrace != null,
                stack: exception.StackTrace,
                parsedStack: System.Array.Empty<StackFrame>());

            detailsList.Add(exceptionDetail);

            // Handle AggregateException specially
            if (exception is AggregateException aggregateException && aggregateException.InnerExceptions != null)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    ConvertExceptionTree(innerException, currentId, detailsList);
                }
            }
            else if (exception.InnerException != null)
            {
                ConvertExceptionTree(exception.InnerException, currentId, detailsList);
            }
        }

        /// <summary>
        /// Reconstructs an exception chain from ExceptionDetailsInfo list using id/outerId structure.
        /// </summary>
        private static Exception ReconstructExceptionFromDetails(IReadOnlyList<ExceptionDetailsInfo> detailsList)
        {
            if (detailsList == null || detailsList.Count == 0)
            {
                return null;
            }

            // Build a dictionary of id -> Exception
            // We need to build from the innermost to outermost
            var exceptionDict = new Dictionary<int, Exception>();
            
            // Sort by id descending so we build inner exceptions first
            var sortedDetails = detailsList.OrderByDescending(d => d.Id).ToList();
            
            foreach (var detail in sortedDetails)
            {
                var message = detail.Message ?? "<no message>";
                Exception innerException = null;
                
                // Find all children (exceptions that have this as outerId)
                var children = detailsList.Where(d => d.OuterId == detail.Id).ToList();
                
                if (children.Count == 1)
                {
                    // Single inner exception
                    innerException = exceptionDict[children[0].Id];
                }
                else if (children.Count > 1)
                {
                    // Multiple inner exceptions - create AggregateException
                    var innerExceptions = children.Select(c => exceptionDict[c.Id]).ToList();
                    innerException = new AggregateException(message, innerExceptions);
                    exceptionDict[detail.Id] = innerException;
                    continue;
                }
                
                // Create exception with inner if it exists
                var exception = innerException != null ? new Exception(message, innerException) : new Exception(message);
                exceptionDict[detail.Id] = exception;
            }

            // Find the root exception (one with outerId == -1)
            var rootDetail = detailsList.FirstOrDefault(d => d.OuterId == -1);
            return rootDetail != null ? exceptionDict[rootDetail.Id] : exceptionDict[detailsList[0].Id];
        }
    }
}
