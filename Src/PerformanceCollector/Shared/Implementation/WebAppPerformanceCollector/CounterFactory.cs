namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;

    /// <summary>
    /// Factory to create different counters.
    /// </summary>
    internal class CounterFactory
    {
        /// <summary>
        /// Gets a counter.
        /// </summary>
        /// <param name="counterName">Name of the counter to retrieve.</param>
        /// <param name="reportAs">Alias to report the counter under.</param>
        /// <returns>The counter identified by counterName</returns>
        public ICounterValue GetCounter(string counterName, string reportAs)
        {
            switch (counterName)
            {
                // Default performance counters
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Execution Time":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "appRequestExecTime",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests In Application Queue":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsInApplicationQueue",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests/Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "requestsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\.NET CLR Exceptions(??APP_CLR_PROC??)\# of Exceps Thrown / sec":
                    return new RateCounterGauge(
                        reportAs,
                        "exceptionsThrown",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\Process(??APP_WIN32_PROC??)\Private Bytes":
                    return new PerformanceCounterFromJsonGauge(
                    reportAs,
                    "privateBytes",
                    AzureWebApEnvironmentVariables.App);
                case @"\Process(?? APP_WIN32_PROC ??)\% Processor Time":
                    return new CPUPercenageGauge(reportAs, 
                        new SumUpGauge(
                            reportAs,
                            new PerformanceCounterFromJsonGauge("kernelTime", "kernelTime", AzureWebApEnvironmentVariables.App),
                            new PerformanceCounterFromJsonGauge("userTime", "userTime", AzureWebApEnvironmentVariables.App)));
                case @"\Process(??APP_WIN32_PROC??)\IO Data Bytes/sec":
                    return new RateCounterGauge(
                        reportAs,
                        "ioDataBytesRate",
                        AzureWebApEnvironmentVariables.App,
                        new SumUpGauge(
                            "ioDataBytesRate",
                            new PerformanceCounterFromJsonGauge(
                                "readIoBytes",
                                "readIoBytes",
                                AzureWebApEnvironmentVariables.App),
                            new PerformanceCounterFromJsonGauge(
                                "writeIoBytes",
                                "writeIoBytes",
                                AzureWebApEnvironmentVariables.App),
                            new PerformanceCounterFromJsonGauge(
                                "otherIoBytes",
                                "otherIoBytes",
                                AzureWebApEnvironmentVariables.App)));

                //$set = Get-Counter -ListSet "ASP.NET Applications"
                //$set.Paths
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Anonymous Requests":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "anonymousRequests",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Anonymous Requests / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "anonymousRequests",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache Total Entries":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "totalCacheEntries",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache Total Turnover Rate":
                    return new RateCounterGauge(
                        reportAs,
                        "totalCacheTurnoverRate",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache Total Hits":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "totalCacheHits",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache Total Misses":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "totalCacheMisses",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache API Entries":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "apiCacheEntries",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache API Turnover Rate":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "apiCacheTurnoverRate",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache API Hits":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "apiCacheHits",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache API Misses":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "apiCacheMisses",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Output Cache Entries":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "outputCacheEntries",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Output Cache Turnover Rate":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "outputCacheTurnoverRate",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Output Cache Hits":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "outputCacheHits",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Output Cache Misses":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "outputCacheMisses",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Compilations Total":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "compilations",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Debugging Requests":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "debuggingRequests",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors During Preprocessing":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "errorsPreProcessing",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors During Compilation":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "errorsCompiling",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors During Execution":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "errorsDuringRequest",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors Unhandled During Execution":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "errorsUnhandled",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors Unhandled During Execution / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "errorsUnhandled",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors Total":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "errorsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors Total / Sec":
                    return new RateCounterGauge(
                    reportAs,
                    "errorsTotal",
                    AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Pipeline Instance Count":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "pipelines",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Bytes In Total":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsBytesIn",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Bytes Out Total":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsBytesOut",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Executing":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsExecuting",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Failed":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsFailed",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Not Found":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsFailed",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Not Authorized":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsNotAuthorized",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Timed Out":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsTimedOut",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Succeeded":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsSucceded",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Total":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "requestsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Sessions Active":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "sessionsActive",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Sessions Abandoned":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "sessionsAbandoned",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Sessions Timed Out":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "sessionsTimedOut",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Sessions Total":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "sessionsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Transactions Aborted":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "transactionsAborted",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Transactions Committed":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "transactionsCommited",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Transactions Pending":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "transactionsPending",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Transactions Total":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "transactionsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Transactions / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "transactionsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Session State Server connections total":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "sessionStateServerConnections",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Session SQL Server connections total":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "sessionSqlServerConnections",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Events Raised":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "eventsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Events Raised / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Application Lifetime Events":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "eventsApp",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Application Lifetime Events / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsApp",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Error Events Raised":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "eventsError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Error Events Raised / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Error Events Raised":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "eventsHttpReqError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Error Events Raised / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsHttpReqError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Infrastructure Error Events Raised":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "eventsHttpInfraError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Infrastructure Error Events Raised / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsHttpInfraError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Events Raised":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "eventsWebReq",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Events Raised / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsWebReq",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Audit Success Events Raised":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "auditSuccess",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Audit Failure Events Raised":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "auditFail",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Membership Authentication Success":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "memberSuccess",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Membership Authentication Failure":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "memberFail",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Forms Authentication Success":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "formsAuthSuccess",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Forms Authentication Failure":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "formsAuthFail",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Viewstate MAC Validation Failure":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "viewstateMacFail",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Disconnected":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "appRequestDisconnected",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Rejected":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "appRequestsRejected",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Wait Time":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "appRequestWaitTime",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache Total Trims":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "cacheTotalTrims",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache API Trims":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "cacheApiTrims",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Output Cache Trims":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "cacheOutputTrims",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\% Managed Processor Time(estimated)":
                    // maybe appCpuUsed and appCpuUsedBase
                    throw new ArgumentException("Performance counter was not found.", counterName);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Managed Memory Used(estimated)":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "appMemoryUsed",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Bytes In Total(WebSockets)":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestBytesInWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Bytes Out Total(WebSockets)":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestBytesOutWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Executing(WebSockets)":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsExecutingWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Failed(WebSockets)":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsFailedWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Succeeded(WebSockets)":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsSucceededWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Total(WebSockets)":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "requestsTotalWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);


                // $set = Get-Counter -ListSet Process
                // $set.Paths
                case @"\Process(??APP_WIN32_PROC??)\% Processor Time":
                    return new CPUPercenageGauge(reportAs, 
                        new SumUpGauge(
                            reportAs,
                            new PerformanceCounterFromJsonGauge("kernelTime", "kernelTime", AzureWebApEnvironmentVariables.App),
                            new PerformanceCounterFromJsonGauge("userTime", "userTime", AzureWebApEnvironmentVariables.App)));
                case @"\Process(??APP_WIN32_PROC??)\% User Time":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "userTime",
                        AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\Page Faults/ sec":
                    return new RateCounterGauge(
                        reportAs,
                        "pageFaults",
                        AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\Thread Count":
                    return new PerformanceCounterFromJsonGauge(
                    reportAs,
                    "threads",
                    AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\Handle Count":
                    return new PerformanceCounterFromJsonGauge(
                    reportAs,
                    "handles",
                    AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\IO Read Operations / sec":
                    return new RateCounterGauge(
                        reportAs,
                        "readIoOperations",
                        AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\IO Write Operations / sec":
                    return new RateCounterGauge(
                        reportAs,
                        "writeIoOperations",
                        AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\IO Other Operations / sec":
                    return new RateCounterGauge(
                        reportAs,
                        "otherIoOperations",
                        AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\IO Read Bytes / sec":
                    return new RateCounterGauge(
                        reportAs,
                        "readIoBytes",
                        AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\IO Write Bytes / sec":
                    return new RateCounterGauge(
                        reportAs,
                        "writeIoBytes",
                        AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\IO Other Bytes / sec":
                    return new RateCounterGauge(
                        reportAs,
                        "otherIoBytes",
                        AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\Working Set - Private":


                //$set = Get - Counter - ListSet ".NET CLR Memory"
                //$set.Paths
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Gen 0 Collections":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "gen0Collections",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Gen 1 Collections":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "gen1Collections",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Gen 2 Collections":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "gen2Collections",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Gen 0 heap size":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "gen0HeapSize",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Gen 1 heap size":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "gen1HeapSize",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Gen 2 heap size":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "gen2HeapSize",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Large Object Heap size":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "largeObjectHeapSize",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Finalization Survivors":
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# GC Handles":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "gcHandles",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Allocated Bytes/ sec":
                    return new RateCounterGauge(
                        reportAs,
                        "allocatedBytes",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Induced GC":
                    return new RateCounterGauge(
                        reportAs,
                        "inducedGC",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Bytes in all Heaps":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "bytesInAllHeaps",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Total committed Bytes":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "commitedBytes",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Total reserved Bytes":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "reservedBytes",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# of Pinned Objects":
                    return new PerformanceCounterFromJsonGauge(
                        reportAs,
                        "pinnedObjects",
                        AzureWebApEnvironmentVariables.CLR);

                default:
                    throw new ArgumentException("Performance counter was not found.", counterName);
            }
        }
    }
}