namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class QuickPulseCollectionStateManager
    {
        private readonly IQuickPulseServiceClient serviceClient = null;

        private readonly Action onStartCollection = null;

        private readonly Action onStopCollection = null;

        private readonly Func<IList<QuickPulseDataSample>> onSubmitSamples = null;

        private readonly Action<IList<QuickPulseDataSample>> onReturnFailedSamples = null;

        public QuickPulseCollectionStateManager(
            IQuickPulseServiceClient serviceClient,
            Action onStartCollection,
            Action onStopCollection,
            Func<IList<QuickPulseDataSample>> onSubmitSamples,
            Action<IList<QuickPulseDataSample>> onReturnFailedSamples)
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

            if (onReturnFailedSamples == null)
            {
                throw new ArgumentNullException(nameof(onReturnFailedSamples));
            }

            this.serviceClient = serviceClient;
            this.onStartCollection = onStartCollection;
            this.onStopCollection = onStopCollection;
            this.onSubmitSamples = onSubmitSamples;
            this.onReturnFailedSamples = onReturnFailedSamples;
        }

        public bool IsCollectingData { get; private set; }

        public void UpdateState(string instrumentationKey)
        {
            if (this.IsCollectingData)
            {
                // we are currently collecting
                // !!! handle back-off
                IList<QuickPulseDataSample> dataSamplesToSubmit = this.onSubmitSamples();
                bool? isCollectingData = dataSamplesToSubmit.Any() ? this.serviceClient.SubmitSamples(dataSamplesToSubmit, instrumentationKey) : true;

                if (isCollectingData == null)
                {
                    // we need to return the samples back to the submitter since we have failed to send them
                    this.onReturnFailedSamples(dataSamplesToSubmit);
                }

                this.IsCollectingData = isCollectingData ?? this.IsCollectingData;

                if (!this.IsCollectingData)
                {
                    // the service wants us to stop collection
                    this.onStopCollection();
                }
            }
            else
            {
                // we are currently idle and pinging the service waiting for it to ask us for data
                // //!!! handle back-off
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