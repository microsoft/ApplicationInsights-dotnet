namespace Microsoft.ApplicationInsights.Extensibility.DependencyCollector.Implementation
{
    using System;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class HttpProcessingFramework : BaseFrameworkProcessing
    {             
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpProcessingFramework"/> class.
        /// </summary>
        internal HttpProcessingFramework(TelemetryClient client, TelemetryConfiguration configuration, ISamplingPolicy samplingPolicy = null)
            : base(samplingPolicy)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            this.telemetryClient = client;
            this.telemetryConfiguration = configuration;
        }
      
        #region Http callbacks
      
        /// <summary>
        /// On begin callback from FX.
        /// </summary>
        /// <param name="id">This object.</param>
        /// <param name="uri">URI of the web request.</param>
        public void OnBeginHttpCallback(long id, string uri)
        {
            if (!RddUtils.IsApplicationInsightsUrl(uri) && !string.IsNullOrEmpty(uri))
            {
                this.OnBegin(id, uri);
            }
        }
        
        /// <summary>
        /// On end callback from FX.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="success">The success to indicate if the dependency call completed successfully or not.</param>
        /// <param name="synchronous">The synchronous flag to indicate if the dependency call was synchronous or not.</param>
        public void OnEndHttpCallback(long id, bool? success, bool? synchronous)
        {
            this.OnEnd(id, success, synchronous);
        }   
        #endregion

        /// <summary>
        /// Gets Dependency Kind for Resource.
        /// </summary>        
        /// <returns>The Dependency Kind.</returns>
        protected override RemoteDependencyKind GetDependencyKind()
        {
            return RemoteDependencyKind.Http;
        }

        /// <summary>
        /// Common helper for all Begin Callbacks.
        /// </summary>
        /// <param name="id">This object.</param>
        /// <param name="resourceName">URI of the web request.</param>
        private void OnBegin(long id, string resourceName)
        {
            try
            {
                if (!this.RddCallCache.Contains(id))
                {
                    DependencyCollectorEventSource.Log.RemoteDependencyModuleVerbose("Http OnBegin id,url:" + id + resourceName);
                    DependencyCallOperation operation = this.Begin(resourceName, this.GetDependencyKind());                    
                    
                    // The callback order is undefined. We use set to prevent collision from previous ids,
                    this.RddCallCache.Set(id, operation);
                }
                else
                {
                    // Not logging as it will result in logging for every outbound AI calls
                }
            }
#pragma warning disable 0168
            catch (Exception exception)
#pragma warning restore 0168
            {
                DependencyCollectorEventSource.Log.RemoteDependencyModuleError(string.Format(CultureInfo.InvariantCulture, "OnBeginHttp failed with {0}.", exception.ToInvariantString()));
            }
        }

        /// <summary>
        /// Common helper for all End Callbacks.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="success">The success to indicate if the dependency call completed successfully or not.</param>
        /// <param name="synchronous">The synchronous flag to indicate if the dependency call was synchronous or not.</param>
        private void OnEnd(long id, bool? success, bool? synchronous)
        {
            var operation = this.RddCallCache.Get(id);
            if (operation != null)
            {
                operation.Telemetry.Async = !synchronous;
            }

            DependencyCollectorEventSource.Log.RemoteDependencyModuleVerbose("Http OnEnd id:" + id);

            // Try to create if sampling this operation
            this.TryCreateTelemetryAndSend(operation, success == null || (bool)success);

            // Remove the item from cache.
            this.RddCallCache.Remove(id);
        }
    }
}
