namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    using Helpers;

    using Microsoft.ApplicationInsights.Extensibility.Filtering;

    internal class QuickPulseCollectionStateManager
    {
        private readonly IQuickPulseServiceClient serviceClient;

        private readonly Clock timeProvider;

        private readonly QuickPulseTimings timings;

        private readonly Action onStartCollection;

        private readonly Action onStopCollection;

        private readonly Func<IList<QuickPulseDataSample>> onSubmitSamples;

        private readonly Action<IList<QuickPulseDataSample>> onReturnFailedSamples;

        private readonly Func<CollectionConfigurationInfo, CollectionConfigurationError[]> onUpdatedConfiguration;

        private readonly TimeSpan coolDownTimeout = TimeSpan.FromMilliseconds(50);

        private readonly List<CollectionConfigurationError> collectionConfigurationErrors = new List<CollectionConfigurationError>();

        private DateTimeOffset lastSuccessfulPing;
        
        private DateTimeOffset lastSuccessfulSubmit;

        private bool isCollectingData;

        private bool firstStateUpdate = true;

        private string currentConfigurationETag = string.Empty;

        public QuickPulseCollectionStateManager(
            IQuickPulseServiceClient serviceClient, 
            Clock timeProvider, 
            QuickPulseTimings timings, 
            Action onStartCollection, 
            Action onStopCollection, 
            Func<IList<QuickPulseDataSample>> onSubmitSamples, 
            Action<IList<QuickPulseDataSample>> onReturnFailedSamples,
            Func<CollectionConfigurationInfo, CollectionConfigurationError[]> onUpdatedConfiguration)
        {
            if (serviceClient == null)
            {
                throw new ArgumentNullException(nameof(serviceClient));
            }

            if (timeProvider == null)
            {
                throw new ArgumentNullException(nameof(timeProvider));
            }

            if (timings == null)
            {
                throw new ArgumentNullException(nameof(timings));
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

            if (onUpdatedConfiguration == null)
            {
                throw new ArgumentNullException(nameof(onUpdatedConfiguration));
            }

            this.serviceClient = serviceClient;
            this.timeProvider = timeProvider;
            this.timings = timings;
            this.onStartCollection = onStartCollection;
            this.onStopCollection = onStopCollection;
            this.onSubmitSamples = onSubmitSamples;
            this.onReturnFailedSamples = onReturnFailedSamples;
            this.onUpdatedConfiguration = onUpdatedConfiguration;
        }
        
        public bool IsCollectingData
        {
            get
            {
                return this.isCollectingData;
            }

            private set
            {
                if (value != this.isCollectingData)
                {
                    // state transition
                    this.ResetLastSuccessful();
                }

                this.isCollectingData = value;
            }
        }

        public TimeSpan UpdateState(string instrumentationKey, string authApiKey)
        {
            if (string.IsNullOrWhiteSpace(instrumentationKey))
            {
                return this.timings.ServicePollingInterval;
            }

            if (this.firstStateUpdate)
            {
                this.ResetLastSuccessful();

                this.firstStateUpdate = false;
            }

            CollectionConfigurationInfo configurationInfo;
            if (this.IsCollectingData)
            {
                // we are currently collecting
                IList<QuickPulseDataSample> dataSamplesToSubmit = this.onSubmitSamples();

                if (!dataSamplesToSubmit.Any())
                {
                    // no samples to submit, do nothing
                    return this.DetermineBackOffs();
                }
                else
                {
                    // we have samples
                    if (dataSamplesToSubmit.Any(sample => sample.CollectionConfigurationAccumulator.GetRef() != 0))
                    {
                        // some samples are still being processed, wait a little to give them a chance to finish
                        Thread.Sleep(this.coolDownTimeout);

                        bool allCooledDown =
                            dataSamplesToSubmit.All(sample => sample.CollectionConfigurationAccumulator.GetRef() == 0);

                        QuickPulseEventSource.Log.CollectionConfigurationSampleCooldownEvent(allCooledDown);
                    }
                }

                bool? keepCollecting = this.serviceClient.SubmitSamples(
                    dataSamplesToSubmit,
                    instrumentationKey,
                    this.currentConfigurationETag,
                    authApiKey,
                    out configurationInfo,
                    this.collectionConfigurationErrors.ToArray());

                QuickPulseEventSource.Log.SampleSubmittedEvent(keepCollecting.ToString());

                switch (keepCollecting)
                {
                    case null:
                        // the request has failed, so we need to return the samples back to the submitter
                        this.onReturnFailedSamples(dataSamplesToSubmit);
                        break;

                    case true:
                        // the service wants us to keep collecting
                        this.UpdateConfiguration(configurationInfo);
                        break;

                    case false:
                        // the service wants us to stop collection
                        this.onStopCollection();
                        break;
                }

                this.lastSuccessfulSubmit = keepCollecting.HasValue ? this.timeProvider.UtcNow : this.lastSuccessfulSubmit;
                this.IsCollectingData = keepCollecting ?? this.IsCollectingData;
            }
            else
            {
                // we are currently idle and pinging the service waiting for it to ask us to start collecting data
                bool? startCollection = this.serviceClient.Ping(
                    instrumentationKey,
                    this.timeProvider.UtcNow,
                    this.currentConfigurationETag,
                    authApiKey,
                    out configurationInfo);

                QuickPulseEventSource.Log.PingSentEvent(startCollection.ToString());

                switch (startCollection)
                {
                    case null:
                        // the request has failed
                        break;

                    case true:
                        // the service wants us to start collection now
                        this.UpdateConfiguration(configurationInfo);
                        this.onStartCollection();
                        break;

                    case false:
                        // the service wants us to remain idle and keep pinging
                        break;
                }

                this.lastSuccessfulPing = startCollection.HasValue ? this.timeProvider.UtcNow : this.lastSuccessfulPing;
                this.IsCollectingData = startCollection ?? this.IsCollectingData;
            }

            return this.DetermineBackOffs();
        }

        private void UpdateConfiguration(CollectionConfigurationInfo configurationInfo)
        {
            // we only get here if Etag in the header is different from the current one, but we still want to check if Etag in the body is also different
            if (configurationInfo != null && !string.Equals(configurationInfo.ETag, this.currentConfigurationETag, StringComparison.Ordinal))
            {
                this.collectionConfigurationErrors.Clear();

                CollectionConfigurationError[] errors = null;
                try
                {
                    errors = this.onUpdatedConfiguration?.Invoke(configurationInfo);
                }
                catch (Exception e)
                {
                    this.collectionConfigurationErrors.Add(
                        CollectionConfigurationError.CreateError(
                            CollectionConfigurationErrorType.CollectionConfigurationFailureToCreateUnexpected,
                            string.Format(CultureInfo.InvariantCulture, "Unexpected error applying configuration. ETag: {0}", configurationInfo.ETag ?? string.Empty),
                            e,
                            Tuple.Create("ETag", configurationInfo.ETag)));
                }

                if (errors != null)
                {
                    this.collectionConfigurationErrors.AddRange(errors);
                }

                this.currentConfigurationETag = configurationInfo.ETag;
            }
        }
        
        private TimeSpan DetermineBackOffs()
        {
            if (this.IsCollectingData)
            {
                TimeSpan timeSinceLastSuccessfulSubmit = this.timeProvider.UtcNow - this.lastSuccessfulSubmit;
                if (timeSinceLastSuccessfulSubmit < this.timings.TimeToCollectionBackOff)
                {
                    return this.timings.CollectionInterval;
                }

                QuickPulseEventSource.Log.TroubleshootingMessageEvent("Collection is failing. Back off.");

                // we have been failing to send samples for a while
                this.onStopCollection();
                this.IsCollectingData = false;

                // we're going back to idling and pinging, but we need to back off immediately
                this.lastSuccessfulPing = DateTimeOffset.MinValue;

                return this.timings.ServicePollingBackedOffInterval;
            }
            else
            {
                TimeSpan timeSinceLastSuccessfulPing = this.timeProvider.UtcNow - this.lastSuccessfulPing;

                return timeSinceLastSuccessfulPing < this.timings.TimeToServicePollingBackOff
                           ? this.timings.ServicePollingInterval
                           : this.timings.ServicePollingBackedOffInterval;
            }
        }

        private void ResetLastSuccessful()
        {
            this.lastSuccessfulPing = this.timeProvider.UtcNow;
            this.lastSuccessfulSubmit = this.timeProvider.UtcNow;
        }
    }
}