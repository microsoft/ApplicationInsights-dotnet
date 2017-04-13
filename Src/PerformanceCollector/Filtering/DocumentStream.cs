namespace Microsoft.ApplicationInsights.Extensibility.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse.Helpers;

    /// <summary>
    /// Represents a concept of a uniquely identifiable set of full telemetry documents that are being reported by the SDK. 
    /// The notion of a stream is needed since multiple UX sessions might be querying for full telemetry documents with 
    /// different filtering criteria simultaneously.
    /// </summary>
    internal class DocumentStream
    {
        private const float MaxTelemetryQuota = 30f;

        private const float InitialTelemetryQuota = 3f;

        private readonly DocumentStreamInfo info;

        private readonly List<FilterConjunctionGroup<RequestTelemetry>> requestFilterGroups = new List<FilterConjunctionGroup<RequestTelemetry>>();

        private readonly List<FilterConjunctionGroup<DependencyTelemetry>> dependencyFilterGroups = new List<FilterConjunctionGroup<DependencyTelemetry>>();

        private readonly List<FilterConjunctionGroup<ExceptionTelemetry>> exceptionFilterGroups = new List<FilterConjunctionGroup<ExceptionTelemetry>>();

        private readonly List<FilterConjunctionGroup<EventTelemetry>> eventFilterGroups = new List<FilterConjunctionGroup<EventTelemetry>>();

        private readonly List<FilterConjunctionGroup<TraceTelemetry>> traceFilterGroups = new List<FilterConjunctionGroup<TraceTelemetry>>();
        
        public DocumentStream(
            DocumentStreamInfo info,
            out CollectionConfigurationError[] errors,
            Clock timeProvider,
            float? initialRequestQuota = null,
            float? initialDependencyQuota = null,
            float? initialExceptionQuota = null,
            float? initialEventQuota = null,
            float? initialTraceQuota = null)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            this.info = info;

            this.CreateFilters(out errors);

            this.RequestQuotaTracker = new QuickPulseQuotaTracker(timeProvider, MaxTelemetryQuota, initialRequestQuota ?? InitialTelemetryQuota);
            this.DependencyQuotaTracker = new QuickPulseQuotaTracker(timeProvider, MaxTelemetryQuota, initialDependencyQuota ?? InitialTelemetryQuota);
            this.ExceptionQuotaTracker = new QuickPulseQuotaTracker(timeProvider, MaxTelemetryQuota, initialExceptionQuota ?? InitialTelemetryQuota);
            this.EventQuotaTracker = new QuickPulseQuotaTracker(timeProvider, MaxTelemetryQuota, initialEventQuota ?? InitialTelemetryQuota);
            this.TraceQuotaTracker = new QuickPulseQuotaTracker(timeProvider, MaxTelemetryQuota, initialTraceQuota ?? InitialTelemetryQuota);
        }

        public QuickPulseQuotaTracker RequestQuotaTracker { get; }

        public QuickPulseQuotaTracker DependencyQuotaTracker { get; }

        public QuickPulseQuotaTracker ExceptionQuotaTracker { get; }

        public QuickPulseQuotaTracker EventQuotaTracker { get; }

        public QuickPulseQuotaTracker TraceQuotaTracker { get; }

        public string Id => this.info.Id;

        public bool CheckFilters(RequestTelemetry document, out CollectionConfigurationError[] errors)
        {
            return DocumentStream.CheckFilters(
                this.requestFilterGroups,
                (filterGroup, errorList) =>
                    {
                        CollectionConfigurationError[] groupErrors;
                        bool groupPassed = filterGroup.CheckFilters(document, out groupErrors);
                        errorList.AddRange(groupErrors ?? new CollectionConfigurationError[0]);

                        return groupPassed;
                    },
                out errors);
        }

        public bool CheckFilters(DependencyTelemetry document, out CollectionConfigurationError[] errors)
        {
            return DocumentStream.CheckFilters(
                this.dependencyFilterGroups,
                (filterGroup, errorList) =>
                    {
                        CollectionConfigurationError[] groupErrors;
                        bool groupPassed = filterGroup.CheckFilters(document, out groupErrors);
                        errorList.AddRange(groupErrors ?? new CollectionConfigurationError[0]);

                        return groupPassed;
                    },
                out errors);
        }

        public bool CheckFilters(ExceptionTelemetry document, out CollectionConfigurationError[] errors)
        {
            return DocumentStream.CheckFilters(
                this.exceptionFilterGroups,
                (filterGroup, errorList) =>
                    {
                        CollectionConfigurationError[] groupErrors;
                        bool groupPassed = filterGroup.CheckFilters(document, out groupErrors);
                        errorList.AddRange(groupErrors ?? new CollectionConfigurationError[0]);

                        return groupPassed;
                    },
                out errors);
        }

        public bool CheckFilters(EventTelemetry document, out CollectionConfigurationError[] errors)
        {
            return DocumentStream.CheckFilters(
                this.eventFilterGroups,
                (filterGroup, errorList) =>
                    {
                        CollectionConfigurationError[] groupErrors;
                        bool groupPassed = filterGroup.CheckFilters(document, out groupErrors);
                        errorList.AddRange(groupErrors ?? new CollectionConfigurationError[0]);

                        return groupPassed;
                    },
                out errors);
        }

        public bool CheckFilters(TraceTelemetry document, out CollectionConfigurationError[] errors)
        {
            return DocumentStream.CheckFilters(
                this.traceFilterGroups,
                (filterGroup, errorList) =>
                {
                    CollectionConfigurationError[] groupErrors;
                    bool groupPassed = filterGroup.CheckFilters(document, out groupErrors);
                    errorList.AddRange(groupErrors ?? new CollectionConfigurationError[0]);

                    return groupPassed;
                },
                out errors);
        }

        private static bool CheckFilters<TTelemetry>(
            List<FilterConjunctionGroup<TTelemetry>> filterGroups,
            Func<FilterConjunctionGroup<TTelemetry>, List<CollectionConfigurationError>, bool> checkFilters,
            out CollectionConfigurationError[] errors)
        {
            errors = new CollectionConfigurationError[0];
            var errorList = new List<CollectionConfigurationError>();
            bool leastOneConjunctionGroupPassed = false;

            if (filterGroups.Count == 0)
            {
                // no filters for the telemetry type - filter out, we're not interested
                return false;
            }

            // iterate over filter groups (filters within each group are evaluated as AND, the groups are evaluated as OR)
            foreach (FilterConjunctionGroup<TTelemetry> conjunctionFilterGroup in filterGroups)
            {
                bool conjunctionGroupPassed;
                try
                {
                    conjunctionGroupPassed = checkFilters(conjunctionFilterGroup, errorList);
                }
                catch (Exception)
                {
                    // the filters have failed to run (possibly incompatible field value in telemetry), consider the telemetry item filtered out by this conjunction group
                    ////!!!
                    ////errorList.Add(
                    ////    CollectionConfigurationError.CreateError(
                    ////        CollectionConfigurationErrorType.DocumentStreamFilterFailureToRun,
                    ////        string.Format(CultureInfo.InvariantCulture, "Document stream filter failed to run"),
                    ////        e));
                    conjunctionGroupPassed = false;
                }

                if (conjunctionGroupPassed)
                {
                    // no need to check remaining groups, one OR-connected group has passed
                    leastOneConjunctionGroupPassed = true;
                    break;
                }
            }

            errors = errorList.ToArray();

            return leastOneConjunctionGroupPassed;
        }

        private void CreateFilters(out CollectionConfigurationError[] errors)
        {
            var errorList = new List<CollectionConfigurationError>();
            foreach (DocumentFilterConjunctionGroupInfo documentFilterConjunctionGroupInfo in this.info.DocumentFilterGroups ?? new DocumentFilterConjunctionGroupInfo[0])
            {
                try
                {
                    CollectionConfigurationError[] groupErrors;
                    switch (documentFilterConjunctionGroupInfo.TelemetryType)
                    {
                        case TelemetryType.Request:
                            this.requestFilterGroups.Add(new FilterConjunctionGroup<RequestTelemetry>(documentFilterConjunctionGroupInfo.Filters, out groupErrors));
                            break;
                        case TelemetryType.Dependency:
                            this.dependencyFilterGroups.Add(new FilterConjunctionGroup<DependencyTelemetry>(documentFilterConjunctionGroupInfo.Filters, out groupErrors));
                            break;
                        case TelemetryType.Exception:
                            this.exceptionFilterGroups.Add(new FilterConjunctionGroup<ExceptionTelemetry>(documentFilterConjunctionGroupInfo.Filters, out groupErrors));
                            break;
                        case TelemetryType.Event:
                            this.eventFilterGroups.Add(new FilterConjunctionGroup<EventTelemetry>(documentFilterConjunctionGroupInfo.Filters, out groupErrors));
                            break;
                        case TelemetryType.Trace:
                            this.traceFilterGroups.Add(new FilterConjunctionGroup<TraceTelemetry>(documentFilterConjunctionGroupInfo.Filters, out groupErrors));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, "Unsupported TelemetryType: '{0}'", documentFilterConjunctionGroupInfo.TelemetryType));
                    }

                    errorList.AddRange(groupErrors);
                }
                catch (Exception e)
                {
                    errorList.Add(
                        CollectionConfigurationError.CreateError(
                            CollectionConfigurationErrorType.DocumentStreamFailureToCreateFilterUnexpected,
                            string.Format(CultureInfo.InvariantCulture, "Failed to create a document stream filter {0}.", documentFilterConjunctionGroupInfo),
                            e,
                            Tuple.Create("DocumentStreamId", this.info.Id)));
                }
            }

            errors = errorList.ToArray();
        }
    }
}