namespace Microsoft.ApplicationInsights.DataContracts
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// Wrapper class for ExceptionData"/> that lets user provide exception data without having the actual Exception object.
    /// </summary>
    internal sealed class ExceptionInfo
    {
        /// <summary>
        /// Constructs the instance of <see cref="ExceptionInfo"/>.
        /// </summary>
        public ExceptionInfo(IEnumerable<ExceptionDetailsInfo> exceptionDetailsInfoList, SeverityLevel? severityLevel, string problemId,
            IDictionary<string, string> properties)
        {
            this.ExceptionDetailsInfoList = exceptionDetailsInfoList != null
                ? new List<ExceptionDetailsInfo>(exceptionDetailsInfoList)
                : new List<ExceptionDetailsInfo>();

            this.SeverityLevel = severityLevel;
            this.ProblemId = problemId;
            this.Properties = properties != null
                ? new ConcurrentDictionary<string, string>(properties)
                : new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Gets a list of <see cref="ExceptionDetailsInfo"/> to modify as needed.
        /// </summary>
        public IReadOnlyList<ExceptionDetailsInfo> ExceptionDetailsInfoList { get; }

        /// <summary>
        /// Gets or sets Exception severity level.
        /// </summary>
        public SeverityLevel? SeverityLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets problem id.
        /// </summary>
        public string ProblemId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets properties collection.
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get;
            set;
        }
    }
}