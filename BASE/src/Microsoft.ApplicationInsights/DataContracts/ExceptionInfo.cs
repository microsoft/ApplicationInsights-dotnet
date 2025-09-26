namespace Microsoft.ApplicationInsights.DataContracts
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Wrapper class for ExceptionData"/> that lets user provide exception data without having the actual Exception object.
    /// </summary>
    internal sealed class ExceptionInfo
    {
        // TODO: fix properties in the constructor

        /// <summary>
        /// Constructs the instance of <see cref="ExceptionInfo"/>.
        /// </summary>
#pragma warning disable CA1801 // Review unused parameters
        public ExceptionInfo(IEnumerable<ExceptionDetailsInfo> exceptionDetailsInfoList, SeverityLevel? severityLevel, string problemId,
            IDictionary<string, string> properties, IDictionary<string, double> measurements)
#pragma warning restore CA1801 // Review unused parameters
        {
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

        /// <summary>
        /// Gets or sets measurements collection.
        /// </summary>
        public IDictionary<string, double> Measurements
        {
            get;
            set;
        }
    }
}