namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
    using System.Threading;

    internal class QuickPulseQuotaTracker
    {
        private const long QuotaScaleFactor = 1000;

        private readonly float inputStreamRatePerSec;

        private readonly long maxQuota;

        private readonly DateTimeOffset startedTrackingTime;

        private readonly Clock timeProvider;

        private long currentQuota;

        private long lastQuotaAccrualFullSeconds;
        
        public QuickPulseQuotaTracker(Clock timeProvider, float maxQuota, float startQuota)
        {
            this.timeProvider = timeProvider;
            this.maxQuota = (long)(QuotaScaleFactor * maxQuota);
            this.inputStreamRatePerSec = maxQuota / 60f;

            this.startedTrackingTime = timeProvider.UtcNow;
            this.lastQuotaAccrualFullSeconds = 0;
            this.currentQuota = (long)(QuotaScaleFactor * startQuota);
        }

        public bool ApplyQuota()
        {
            var currentTimeFullSeconds = (long)(this.timeProvider.UtcNow - this.startedTrackingTime).TotalSeconds;

            this.AccrueQuota(currentTimeFullSeconds);

            return this.UseQuota();
        }

        private bool UseQuota()
        {
            long originalValue = Interlocked.Read(ref this.currentQuota);

            if (originalValue < QuotaScaleFactor)
            {
                return false;
            }

            long newValue = Interlocked.Add(ref this.currentQuota, -QuotaScaleFactor);

            if (newValue < 0)
            {
                // other threads have exhausted the quota since we read it last
                // correct the mistake, but note that we may get incorrect result for some calls to UseQuota
                Interlocked.Add(ref this.currentQuota, QuotaScaleFactor);
            }

            return true;
        }

        private void AccrueQuota(long currentTimeFullSeconds)
        {
            var spin = new SpinWait();

            while (true)
            {
                long lastQuotaAccrualFullSecondsLocal = Interlocked.Read(ref this.lastQuotaAccrualFullSeconds);

                long fullSecondsSinceLastQuotaAccrual = currentTimeFullSeconds - lastQuotaAccrualFullSecondsLocal;

                if (fullSecondsSinceLastQuotaAccrual > 0)
                {
                    // we are in a new second (possibly along with a bunch of competing threads, some of which might actually be in different (also new) seconds)
                    // only one thread will succeed in updating this.lastQuotaAccrualFullSeconds
                    long newValue = lastQuotaAccrualFullSecondsLocal + fullSecondsSinceLastQuotaAccrual;

                    long valueBeforeExchange = Interlocked.CompareExchange(
                        ref this.lastQuotaAccrualFullSeconds,
                        newValue,
                        lastQuotaAccrualFullSecondsLocal);

                    if (valueBeforeExchange == lastQuotaAccrualFullSecondsLocal)
                    {
                        // we have updated this.lastQuotaAccrualFullSeconds, now increase the quota value
                        this.IncreaseQuota(fullSecondsSinceLastQuotaAccrual);

                        break;
                    }
                    else if (valueBeforeExchange >= newValue)
                    {
                        // a thread that was in a later (or same) second has beaten us to updating the value
                        // we don't have to do anything since the time that has passed between the previous
                        // update and this thread's current time has already been accounted for by that other thread
                        break;
                    }
                    else
                    {
                        // a thread that was in an earlier second (but still a later one compared to the previous update) has beaten us to updating the value
                        // we have to repeat the attempt to account for the time that has passed since
                    }
                }
                else
                {
                    // we're within a second that has already been accounted for by another thread, do nothing
                    break;
                }

                spin.SpinOnce();
            }
        }

        private void IncreaseQuota(long seconds)
        {
            long delta = (long)(QuotaScaleFactor * this.inputStreamRatePerSec * seconds);

            long newValue = Interlocked.Add(ref this.currentQuota, delta);

            if (newValue > this.maxQuota)
            {
                Interlocked.Exchange(ref this.currentQuota, this.maxQuota);
            }
        }
    }
}