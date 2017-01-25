namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Data.SqlClient;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Concrete class with all processing logic to generate RDD data from the calls backs
    /// received from Profiler instrumentation for SQL Connection.    
    /// </summary>
    internal sealed class ProfilerSqlConnectionProcessing : ProfilerSqlProcessingBase
    {
        /// <summary>
        /// Constant command text to return.
        /// </summary> 
        private const string SqlConnectionCommandText = "Open";

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilerSqlConnectionProcessing"/> class.
        /// </summary>
        internal ProfilerSqlConnectionProcessing(TelemetryConfiguration configuration, string agentVersion, ObjectInstanceBasedOperationHolder telemetryTupleHolder)
            : base(configuration, agentVersion, telemetryTupleHolder)
        {
        }              

        /// <summary>
        /// Gets SQL connection resource name.
        /// </summary>
        /// <param name="thisObj">The SQL connection.</param>
        /// <remarks>Before we have clarity with SQL team around EventSource instrumentation, providing name as a concatenation of parameters.</remarks>
        /// <returns>The resource name if possible otherwise empty string.</returns>
        internal override string GetResourceName(object thisObj)
        {
            return this.GetResourceNameInternal(thisObj);
        }

        /// <summary>
        /// Gets SQL connection resource target name.
        /// </summary>
        /// <param name="thisObj">The SQL connection.</param>
        /// <returns>The resource target name if possible otherwise empty string.</returns>
        internal override string GetResourceTarget(object thisObj)
        {
            return this.GetResourceNameInternal(thisObj);
        }

        /// <summary>
        /// Gets SQL connection command text.
        /// </summary>
        /// <param name="thisObj">The SQL connection.</param>
        /// <returns>Returns predefined command text.</returns>
        internal override string GetCommandName(object thisObj)
        {            
            return SqlConnectionCommandText;
        }

        private string GetResourceNameInternal(object thisObj)
        {
            string resource = string.Empty;

            SqlConnection connection = thisObj as SqlConnection;            
            if (connection != null)
            {
                resource = string.Join(" | ", connection.DataSource, connection.Database);
            }

            return resource;
        }
    }
}