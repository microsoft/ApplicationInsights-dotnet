namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
    using System.Collections.Generic;

    internal class QuickPulseCollectionStateManager
    {
        private readonly IQuickPulseServiceClient serviceClient = null;
        private readonly Action onStartCollection = null;
        private readonly Action onStopCollection = null;
        private readonly Func<IEnumerable<QuickPulseDataSample>> onSubmitSamples = null;
        
        public QuickPulseCollectionStateManager(IQuickPulseServiceClient serviceClient, Action onStartCollection, Action onStopCollection, Func<IEnumerable<QuickPulseDataSample>> onSubmitSamples)
        {
            if (serviceClient == null)
            {
                throw new ArgumentNullException(nameof(serviceClient));
            }

            if (onStartCollection == null)
            {
                throw new ArgumentNullException(nameof(onStartCollection));
            }

            if (onStopCollection == null)
            {
                throw new ArgumentNullException(nameof(onStopCollection));
            }

            if (onSubmitSamples == null)
            {
                throw new ArgumentNullException(nameof(onSubmitSamples));
            }

            this.serviceClient = serviceClient;
            this.onStartCollection = onStartCollection;
            this.onStopCollection = onStopCollection;
            this.onSubmitSamples = onSubmitSamples;
        }

        public bool IsCollectingData { get; private set; }

        public void UpdateState(string instrumentationKey)
        {
            if (this.IsCollectingData)
            {
                // we are currently collecting
                // //!!! stay in the same state if can't get response
                this.IsCollectingData = this.serviceClient.SubmitSamples(this.onSubmitSamples(), instrumentationKey) ?? this.IsCollectingData;

                if (!this.IsCollectingData)
                {
                    // the service wants us to stop collection
                    this.onStopCollection();
                }
            }
            else
            {
                // we are currently idle and pinging the service waiting for it to ask us for data
                // //!!! stay in the same state if can't get response
                this.IsCollectingData = this.serviceClient.Ping(instrumentationKey) ?? this.IsCollectingData;

                if (this.IsCollectingData)
                {
                    // the service wants us to start collection now
                    this.onStartCollection();
                }
                else
                {
                    // the service wants us to remain idle and keep pinging
                }
            }
        }
    }
}