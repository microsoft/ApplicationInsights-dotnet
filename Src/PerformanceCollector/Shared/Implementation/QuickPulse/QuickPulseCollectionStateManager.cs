namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;

    internal class QuickPulseCollectionStateManager
    {
        private readonly IQuickPulseServiceClient serviceClient = null;
        private readonly Action onStartCollection = null;
        private readonly Action onStopCollection = null;
        private readonly Func<bool> onCollect = null;

        public QuickPulseCollectionStateManager(IQuickPulseServiceClient serviceClient, Action onStartCollection, Action onStopCollection, Func<bool> onCollect)
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

            if (onCollect == null)
            {
                throw new ArgumentNullException(nameof(onCollect));
            }

            this.serviceClient = serviceClient;
            this.onStartCollection = onStartCollection;
            this.onStopCollection = onStopCollection;
            this.onCollect = onCollect;
        }

        public bool IsCollectingData { get; private set; }

        public void PerformAction()
        {
            if (this.IsCollectingData)
            {
                // we are currently collecting
                this.IsCollectingData = this.onCollect();

                if (!this.IsCollectingData)
                {
                    // the service wants us to stop collection
                    this.onStopCollection();
                }
            }
            else
            {
                // we are currently idle and pinging the service waiting for it to ask us for data
                this.IsCollectingData = this.serviceClient.Ping();

                if (this.IsCollectingData)
                {
                    // the service wants us to start collection now
                    this.onStartCollection();

                    this.IsCollectingData = this.onCollect();

                    if (!this.IsCollectingData)
                    {
                        // the service wants us to stop collecting now (after a single collection)
                        this.onStopCollection();
                    }
                }
                else
                {
                    // the service wants us to remain idle and keep pinging
                }
            }
        }
    }
}