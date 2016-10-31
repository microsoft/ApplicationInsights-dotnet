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
        /// <returns>The counter identified by counter name.</returns>
        public ICounterValue GetCounter(string counterName, string reportAs)
        {
            switch (counterName)
            {
                // Default performance counters
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Execution Time":
                    return new RawCounterGauge(
                        reportAs,
                        "appRequestExecTime",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests In Application Queue":
                    return new RawCounterGauge(
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
                    return new RawCounterGauge(
                    reportAs,
                    "privateBytes",
                    AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\IO Data Bytes/sec":
                    return new RateCounterGauge(
                        reportAs,
                        "ioDataBytesRate",
                        AzureWebApEnvironmentVariables.App,
                        new SumUpCountersGauge(
                            "ioDataBytesRate",
                            new RawCounterGauge(
                                "readIoBytes",
                                "readIoBytes",
                                AzureWebApEnvironmentVariables.App),
                            new RawCounterGauge(
                                "writeIoBytes",
                                "writeIoBytes",
                                AzureWebApEnvironmentVariables.App),
                            new RawCounterGauge(
                                "otherIoBytes",
                                "otherIoBytes",
                                AzureWebApEnvironmentVariables.App)));

                ////$set = Get-Counter -ListSet "ASP.NET Applications"
                ////$set.Paths
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Anonymous Requests":
                    return new RawCounterGauge(
                        reportAs,
                        "anonymousRequests",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Anonymous Requests / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "anonymousRequests",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache Total Entries":
                    return new RawCounterGauge(
                        reportAs,
                        "totalCacheEntries",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache Total Turnover Rate":
                    return new RateCounterGauge(
                        reportAs,
                        "totalCacheTurnoverRate",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache Total Hits":
                    return new RawCounterGauge(
                        reportAs,
                        "totalCacheHits",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache Total Misses":
                    return new RawCounterGauge(
                        reportAs,
                        "totalCacheMisses",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache API Entries":
                    return new RawCounterGauge(
                        reportAs,
                        "apiCacheEntries",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache API Turnover Rate":
                    return new RawCounterGauge(
                        reportAs,
                        "apiCacheTurnoverRate",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache API Hits":
                    return new RawCounterGauge(
                        reportAs,
                        "apiCacheHits",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache API Misses":
                    return new RawCounterGauge(
                        reportAs,
                        "apiCacheMisses",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Output Cache Entries":
                    return new RawCounterGauge(
                        reportAs,
                        "outputCacheEntries",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Output Cache Turnover Rate":
                    return new RawCounterGauge(
                        reportAs,
                        "outputCacheTurnoverRate",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Output Cache Hits":
                    return new RawCounterGauge(
                        reportAs,
                        "outputCacheHits",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Output Cache Misses":
                    return new RawCounterGauge(
                        reportAs,
                        "outputCacheMisses",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Compilations Total":
                    return new RawCounterGauge(
                        reportAs,
                        "compilations",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Debugging Requests":
                    return new RawCounterGauge(
                        reportAs,
                        "debuggingRequests",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors During Preprocessing":
                    return new RawCounterGauge(
                        reportAs,
                        "errorsPreProcessing",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors During Compilation":
                    return new RawCounterGauge(
                        reportAs,
                        "errorsCompiling",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors During Execution":
                    return new RawCounterGauge(
                        reportAs,
                        "errorsDuringRequest",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors Unhandled During Execution":
                    return new RawCounterGauge(
                        reportAs,
                        "errorsUnhandled",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors Unhandled During Execution / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "errorsUnhandled",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors Total":
                    return new RawCounterGauge(
                        reportAs,
                        "errorsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Errors Total / Sec":
                    return new RateCounterGauge(
                    reportAs,
                    "errorsTotal",
                    AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Pipeline Instance Count":
                    return new RawCounterGauge(
                        reportAs,
                        "pipelines",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Bytes In Total":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsBytesIn",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Bytes Out Total":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsBytesOut",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Executing":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsExecuting",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Failed":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsFailed",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Not Found":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsFailed",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Not Authorized":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsNotAuthorized",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Timed Out":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsTimedOut",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Succeeded":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsSucceded",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Total":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "requestsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Sessions Active":
                    return new RawCounterGauge(
                        reportAs,
                        "sessionsActive",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Sessions Abandoned":
                    return new RawCounterGauge(
                        reportAs,
                        "sessionsAbandoned",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Sessions Timed Out":
                    return new RawCounterGauge(
                        reportAs,
                        "sessionsTimedOut",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Sessions Total":
                    return new RawCounterGauge(
                        reportAs,
                        "sessionsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Transactions Aborted":
                    return new RawCounterGauge(
                        reportAs,
                        "transactionsAborted",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Transactions Committed":
                    return new RawCounterGauge(
                        reportAs,
                        "transactionsCommited",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Transactions Pending":
                    return new RawCounterGauge(
                        reportAs,
                        "transactionsPending",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Transactions Total":
                    return new RawCounterGauge(
                        reportAs,
                        "transactionsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Transactions / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "transactionsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Session State Server connections total":
                    return new RawCounterGauge(
                        reportAs,
                        "sessionStateServerConnections",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Session SQL Server connections total":
                    return new RawCounterGauge(
                        reportAs,
                        "sessionSqlServerConnections",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Events Raised":
                    return new RawCounterGauge(
                        reportAs,
                        "eventsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Events Raised / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsTotal",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Application Lifetime Events":
                    return new RawCounterGauge(
                        reportAs,
                        "eventsApp",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Application Lifetime Events / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsApp",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Error Events Raised":
                    return new RawCounterGauge(
                        reportAs,
                        "eventsError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Error Events Raised / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Error Events Raised":
                    return new RawCounterGauge(
                        reportAs,
                        "eventsHttpReqError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Error Events Raised / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsHttpReqError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Infrastructure Error Events Raised":
                    return new RawCounterGauge(
                        reportAs,
                        "eventsHttpInfraError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Infrastructure Error Events Raised / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsHttpInfraError",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Events Raised":
                    return new RawCounterGauge(
                        reportAs,
                        "eventsWebReq",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Events Raised / Sec":
                    return new RateCounterGauge(
                        reportAs,
                        "eventsWebReq",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Audit Success Events Raised":
                    return new RawCounterGauge(
                        reportAs,
                        "auditSuccess",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Audit Failure Events Raised":
                    return new RawCounterGauge(
                        reportAs,
                        "auditFail",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Membership Authentication Success":
                    return new RawCounterGauge(
                        reportAs,
                        "memberSuccess",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Membership Authentication Failure":
                    return new RawCounterGauge(
                        reportAs,
                        "memberFail",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Forms Authentication Success":
                    return new RawCounterGauge(
                        reportAs,
                        "formsAuthSuccess",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Forms Authentication Failure":
                    return new RawCounterGauge(
                        reportAs,
                        "formsAuthFail",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Viewstate MAC Validation Failure":
                    return new RawCounterGauge(
                        reportAs,
                        "viewstateMacFail",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Disconnected":
                    return new RawCounterGauge(
                        reportAs,
                        "appRequestDisconnected",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Rejected":
                    return new RawCounterGauge(
                        reportAs,
                        "appRequestsRejected",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Wait Time":
                    return new RawCounterGauge(
                        reportAs,
                        "appRequestWaitTime",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache Total Trims":
                    return new RawCounterGauge(
                        reportAs,
                        "cacheTotalTrims",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Cache API Trims":
                    return new RawCounterGauge(
                        reportAs,
                        "cacheApiTrims",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Output Cache Trims":
                    return new RawCounterGauge(
                        reportAs,
                        "cacheOutputTrims",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\% Managed Processor Time(estimated)":
                    // maybe appCpuUsed and appCpuUsedBase
                    throw new ArgumentException("Performance counter was not found.", counterName);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Managed Memory Used(estimated)":
                    return new RawCounterGauge(
                        reportAs,
                        "appMemoryUsed",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Bytes In Total(WebSockets)":
                    return new RawCounterGauge(
                        reportAs,
                        "requestBytesInWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Bytes Out Total(WebSockets)":
                    return new RawCounterGauge(
                        reportAs,
                        "requestBytesOutWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Executing(WebSockets)":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsExecutingWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Failed(WebSockets)":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsFailedWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Succeeded(WebSockets)":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsSucceededWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);
                case @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests Total(WebSockets)":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsTotalWebsockets",
                        AzureWebApEnvironmentVariables.AspNet);

                //// $set = Get-Counter -ListSet Process
                //// $set.Paths
                case @"\Process(??APP_WIN32_PROC??)\% Processor Time":
                    return new CPUPercenageGauge(
                        reportAs, 
                        new SumUpCountersGauge(
                            reportAs,
                            new RawCounterGauge("kernelTime", "kernelTime", AzureWebApEnvironmentVariables.App),
                            new RawCounterGauge("userTime", "userTime", AzureWebApEnvironmentVariables.App)));
                case @"\Process(??APP_WIN32_PROC??)\% User Time":
                    return new RawCounterGauge(
                        reportAs,
                        "userTime",
                        AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\Page Faults/ sec":
                    return new RateCounterGauge(
                        reportAs,
                        "pageFaults",
                        AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\Thread Count":
                    return new RawCounterGauge(
                    reportAs,
                    "threads",
                    AzureWebApEnvironmentVariables.App);
                case @"\Process(??APP_WIN32_PROC??)\Handle Count":
                    return new RawCounterGauge(
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

                ////$set = Get - Counter - ListSet ".NET CLR Memory"
                ////$set.Paths
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Gen 0 Collections":
                    return new RawCounterGauge(
                        reportAs,
                        "gen0Collections",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Gen 1 Collections":
                    return new RawCounterGauge(
                        reportAs,
                        "gen1Collections",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Gen 2 Collections":
                    return new RawCounterGauge(
                        reportAs,
                        "gen2Collections",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Gen 0 heap size":
                    return new RawCounterGauge(
                        reportAs,
                        "gen0HeapSize",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Gen 1 heap size":
                    return new RawCounterGauge(
                        reportAs,
                        "gen1HeapSize",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Gen 2 heap size":
                    return new RawCounterGauge(
                        reportAs,
                        "gen2HeapSize",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Large Object Heap size":
                    return new RawCounterGauge(
                        reportAs,
                        "largeObjectHeapSize",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\Finalization Survivors":
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# GC Handles":
                    return new RawCounterGauge(
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
                    return new RawCounterGauge(
                        reportAs,
                        "bytesInAllHeaps",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Total committed Bytes":
                    return new RawCounterGauge(
                        reportAs,
                        "commitedBytes",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# Total reserved Bytes":
                    return new RawCounterGauge(
                        reportAs,
                        "reservedBytes",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\.NET CLR Memory(??APP_CLR_PROC??)\# of Pinned Objects":
                    return new RawCounterGauge(
                        reportAs,
                        "pinnedObjects",
                        AzureWebApEnvironmentVariables.CLR);
                
                //// Quick pulse related hard coded performance counters.
                case @"\Memory\Committed Bytes":
                    return new RawCounterGauge(
                        reportAs,
                        "committedBytes",
                        AzureWebApEnvironmentVariables.CLR);
                case @"\Processor(_Total)\% Processor Time":
                    return new CPUPercenageGauge(
                        reportAs, 
                        new SumUpCountersGauge(
                            reportAs,
                            new RawCounterGauge("kernelTime", "kernelTime", AzureWebApEnvironmentVariables.App),
                            new RawCounterGauge("userTime", "userTime", AzureWebApEnvironmentVariables.App)));
                case @"\ASP.NET Applications(__Total__)\Requests In Application Queue":
                    return new RawCounterGauge(
                        reportAs,
                        "requestsInApplicationQueue",
                        AzureWebApEnvironmentVariables.AspNet);
                default:
                    throw new ArgumentException("Performance counter was not found.", counterName);
            }
        }
    }
}