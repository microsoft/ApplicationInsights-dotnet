namespace Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId
{
    using System.Collections.Generic;

    /// <summary>
    /// Application Id Provider that holds a dictionary of Instrumentation Keys to Application Ids.
    /// </summary>
    public class DictionaryApplicationIdProvider : IApplicationIdProvider
    {
        /// <summary>
        /// Gets or sets a dictionary of Instrumentation Keys to Application Ids
        /// </summary>
        public Dictionary<string, string> Defined { get; set; }

        /// <summary>
        /// Provides an Application Id based on an Instrumentation Key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation Key string used to lookup associated Application Id.</param>
        /// <param name="applicationId">Application Id corresponding to the Instrumentation Key</param>
        /// <returns>TRUE if Application Id was successfully retrieved, FALSE otherwise.</returns>
        public bool TryGetApplicationId(string instrumentationKey, out string applicationId)
        {
            if (this.Defined == null)
            {
                applicationId = null;
                return false;
            }

            return this.Defined.TryGetValue(instrumentationKey, out applicationId);
        }
    }
}
