namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// Concrete class with all processing logic to generate RDD data from the calls backs
    /// received from Profiler instrumentation for SQL.    
    /// </summary>
    internal sealed class ProfilerSqlProcessing
    {
        internal ObjectInstanceBasedOperationHolder TelemetryTable;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilerSqlProcessing"/> class.
        /// </summary>
        internal ProfilerSqlProcessing(TelemetryConfiguration configuration, string agentVersion, ObjectInstanceBasedOperationHolder telemetryTupleHolder)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (telemetryTupleHolder == null)
            {
                throw new ArgumentNullException("telemetryHolder");
            }

            this.TelemetryTable = telemetryTupleHolder;
            this.telemetryClient = new TelemetryClient(configuration);
           
            // Since dependencySource is no longer set, sdk version is prepended with information which can identify whether RDD was collected by profiler/framework
           
            // For directly using TrackDependency(), version will be simply what is set by core
            string prefix = "rdd" + RddSource.Profiler + ":";
            this.telemetryClient.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion(prefix);
            if (!string.IsNullOrEmpty(agentVersion))
            {
                this.telemetryClient.Context.GetInternalContext().AgentVersion = agentVersion;
            }
        }

        #region Sql callbacks

        /// <summary>
        /// On begin callback for methods with 1 parameter.
        /// </summary>
        public object OnBeginForOneParameter(object thisObj)
        {
            return this.OnBegin(thisObj);
        }

        /// <summary>
        /// On begin callback for methods with 2 parameter.
        /// </summary>
        public object OnBeginForTwoParameters(object thisObj, object parameter1)
        {
            return this.OnBegin(thisObj);
        }

        /// <summary>
        /// On begin callback for methods with 3 parameters.
        /// </summary>
        public object OnBeginForThreeParameters(object thisObj, object parameter1, object parameter2)
        {
            return this.OnBegin(thisObj);
        }

        /// <summary>
        /// On begin callback for methods with 4 parameter.
        /// </summary>
        public object OnBeginForFourParameters(object thisObj, object parameter1, object parameter2, object parameter3)
        {
            return this.OnBegin(thisObj);
        }

        /// <summary>
        /// On end callback for methods with 1 parameter.
        /// </summary>
        public object OnEndForOneParameter(object context, object returnValue, object thisObj)
        {
            this.OnEnd(null, thisObj);
            return returnValue;
        }

        /// <summary>
        /// On end callback for methods with 2 parameter.
        /// </summary>
        public object OnEndForTwoParameters(object context, object returnValue, object thisObj, object parameter1)
        {
            this.OnEnd(null, thisObj);
            return returnValue;
        }

        /// <summary>
        /// On end callback for methods with 3 parameter.
        /// </summary>
        public object OnEndForThreeParameters(object context, object returnValue, object thisObj, object parameter1, object parameter2)
        {
            this.OnEnd(null, thisObj);
            return returnValue;
        }

        /// <summary>
        /// On exception callback for methods with 1 parameter.
        /// </summary>
        public void OnExceptionForOneParameter(object context, object exception, object thisObj)
        {
            this.OnEnd(exception, thisObj);
        }

        /// <summary>
        /// On exception callback for methods with 2 parameter.
        /// </summary>
        public void OnExceptionForTwoParameters(object context, object exception, object thisObj, object parameter1)
        {
            this.OnEnd(exception, thisObj);
        }

        /// <summary>
        /// On exception callback for methods with 3 parameter.
        /// </summary>
        public void OnExceptionForThreeParameters(object context, object exception, object thisObj, object parameter1, object parameter2)
        {
            this.OnEnd(exception, thisObj);
        }

        #endregion //Sql callbacks

        /// <summary>
        /// Gets SQL command resource name.
        /// </summary>
        /// <param name="thisObj">The SQL command.</param>
        /// <remarks>Before we have clarity with SQL team around EventSource instrumentation, providing name as a concatenation of parameters.</remarks>
        /// <returns>The resource name if possible otherwise empty string.</returns>
        internal string GetResourceName(object thisObj)
        {
            SqlCommand command = thisObj as SqlCommand;
            string resource = string.Empty;
            if (command != null)
            {
                if (command.Connection != null)
                {
                    string commandName = command.CommandType == CommandType.StoredProcedure
                        ? command.CommandText
                        : string.Empty;

                    resource = string.IsNullOrEmpty(commandName)
                        ? string.Join(" | ", command.Connection.DataSource, command.Connection.Database)
                        : string.Join(" | ", command.Connection.DataSource, command.Connection.Database, commandName);
                }
            }

            return resource;
        }

        internal string GetResourceTarget(object thisObj)
        {
            SqlCommand command = thisObj as SqlCommand;
            string result = string.Empty;
            if (command != null)
            {
                if (command.Connection != null)
                {
                    result = string.Join(" | ", command.Connection.DataSource, command.Connection.Database);
                }
            }

            return result;
        }

        /// <summary>
        /// Return CommandTest for SQL resource.
        /// </summary>
        /// <param name="thisObj">The SQL command.</param>
        /// <returns>Returns the command text or empty.</returns>
        internal string GetCommandName(object thisObj)
        {
            SqlCommand command = thisObj as SqlCommand;

            if (command != null)
            {
                return command.CommandText ?? string.Empty;
            }

            return string.Empty;
        }
     
        /// <summary>
        ///  Common helper for all Begin Callbacks.
        /// </summary>
        /// <param name="thisObj">This object.</param>
        /// <returns>The context for end callback.</returns>
        private object OnBegin(object thisObj)
        {
            try
            {
                if (thisObj == null)
                {
                    DependencyCollectorEventSource.Log.NotExpectedCallback(0, "OnBeginSql", "thisObj == null");
                    return null;
                }

                string resourceName = this.GetResourceName(thisObj);
                DependencyCollectorEventSource.Log.BeginCallbackCalled(thisObj.GetHashCode(), resourceName);

                if (string.IsNullOrEmpty(resourceName))
                {
                    DependencyCollectorEventSource.Log.NotExpectedCallback(thisObj.GetHashCode(), "OnBeginSql", "resourceName is empty");
                    return null;
                }

                var telemetryTuple = this.TelemetryTable.Get(thisObj);
                if (telemetryTuple != null)
                {
                    // We are already tracking this item
                    if (telemetryTuple.Item1 != null)
                    {
                        DependencyCollectorEventSource.Log.TrackingAnExistingTelemetryItemVerbose();
                        return null;
                    }
                }

                string commandText = this.GetCommandName(thisObj);

                // Try to begin if sampling this operation
                bool isCustomCreated = false;
                var telemetry = ClientServerDependencyTracker.BeginTracking(this.telemetryClient);

                telemetry.Name = resourceName;
                telemetry.Type = RemoteDependencyKind.SQL.ToString();
                telemetry.Target = this.GetResourceTarget(thisObj);
                telemetry.Data = commandText;

                // We use weaktables to store the thisObj for correlating begin with end call.
                this.TelemetryTable.Store(thisObj, new Tuple<DependencyTelemetry, bool>(telemetry, isCustomCreated));
                return null;
            }
            catch (Exception exception)
            {
                DependencyCollectorEventSource.Log.CallbackError(thisObj == null ? 0 : thisObj.GetHashCode(), "OnBeginSql", exception);
            }

            return null;
        }

        /// <summary>
        ///  Common helper for all End Callbacks.
        /// </summary>
        /// <param name="exceptionObj">The exception object if any.</param>
        /// <param name="thisObj">This object.</param>
        private void OnEnd(object exceptionObj, object thisObj)
        {
            try
            {
                if (thisObj == null)
                {
                    DependencyCollectorEventSource.Log.NotExpectedCallback(0, "OnEndSql", "thisObj == null");
                    return;
                }

                DependencyCollectorEventSource.Log.EndCallbackCalled(thisObj.GetHashCode().ToString(CultureInfo.InvariantCulture));

                DependencyTelemetry telemetry = null;
                Tuple<DependencyTelemetry, bool> telemetryTuple = null;
                bool isCustomGenerated = false;

                telemetryTuple = this.TelemetryTable.Get(thisObj);
                if (telemetryTuple != null)
                {
                    telemetry = telemetryTuple.Item1;
                    isCustomGenerated = telemetryTuple.Item2;
                }

                if (telemetry == null)
                {
                    DependencyCollectorEventSource.Log.EndCallbackWithNoBegin(thisObj.GetHashCode().ToString(CultureInfo.InvariantCulture));
                    return;
                }

                if (!isCustomGenerated)
                {
                    this.TelemetryTable.Remove(thisObj);

                    var exception = exceptionObj as Exception;
                    if (exception != null)
                    {
                        telemetry.Success = false;
                        telemetry.Properties.Add("ErrorMessage", exception.Message);

                        var sqlEx = exception as SqlException;
                        telemetry.ResultCode = sqlEx != null ? sqlEx.Number.ToString(CultureInfo.InvariantCulture) : "0";
                    }
                    else
                    {
                        telemetry.Success = true;
                        telemetry.ResultCode = "0";
                    }

                    ClientServerDependencyTracker.EndTracking(this.telemetryClient, telemetry);
                }               
            }
            catch (Exception ex)
            {
                DependencyCollectorEventSource.Log.CallbackError(thisObj == null ? 0 : thisObj.GetHashCode(), "OnEndSql", ex);
            }
        }
    }
}