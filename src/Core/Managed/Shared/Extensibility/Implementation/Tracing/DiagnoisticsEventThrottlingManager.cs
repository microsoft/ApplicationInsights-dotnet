// -----------------------------------------------------------------------
// <copyright file="DiagnoisticsEventThrottlingManager.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// <summary></summary>
// -----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;

    internal class DiagnoisticsEventThrottlingManager<T> : IDiagnoisticsEventThrottlingManager
        where T : IDiagnoisticsEventThrottling
    {
        private readonly T snapshotContainer;

        internal DiagnoisticsEventThrottlingManager(
            T snapshotContainer, 
            IDiagnoisticsEventThrottlingScheduler scheduler,
            uint throttlingRecycleIntervalInMinutes)
        {
            if (null == snapshotContainer)
            {
                throw new ArgumentNullException("snapshotContainer");
            }

            if (null == scheduler)
            {
                throw new ArgumentNullException("scheduler");
            }

            if (false == throttlingRecycleIntervalInMinutes.IsInRangeThrottlingRecycleInterval())
            {
                throw new ArgumentOutOfRangeException("throttlingRecycleIntervalInMinutes");
            }

            this.snapshotContainer = snapshotContainer;

            var throttlingRecycleIntervalInMilliseconds = (int)throttlingRecycleIntervalInMinutes * 60 * 1000;
            scheduler.ScheduleToRunEveryTimeIntervalInMilliseconds(
                throttlingRecycleIntervalInMilliseconds,
                this.ResetThrottling);
        }

        public bool ThrottleEvent(int eventId, long keywords)
        {
            bool justExceededThreshold;
            
            var throttleEvent = this.snapshotContainer.ThrottleEvent(
                eventId,
                keywords,
                out justExceededThreshold);

            if (true == justExceededThreshold)
            {
                CoreEventSource.Log.DiagnosticsEventThrottlingHasBeenStartedForTheEvent(
                    eventId);
            }

            return throttleEvent;
        }

        private void ResetThrottling()
        {
            var snapshot = this.snapshotContainer.CollectSnapshot();

            foreach (var record in snapshot)
            {
                CoreEventSource.Log.DiagnosticsEventThrottlingHasBeenResetForTheEvent(
                    record.Key, 
                    record.Value.ExecCount);
            }
        }
    }
}