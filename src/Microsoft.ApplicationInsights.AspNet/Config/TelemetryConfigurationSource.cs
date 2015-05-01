namespace Microsoft.ApplicationInsights.AspNet.Config
{
    using Microsoft.Framework.ConfigurationModel;
    using System;
    using System.Collections.Generic;

    internal class TelemetryConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Developer mode setting value.
        /// </summary>
        public bool? DeveloperMode { get; set; }

        /// <summary>
        /// Instrumentation key setting value.
        /// </summary>
        public string InstrumentationKey { get; set; }

        /// <summary>
        /// Endpoint address setting value.
        /// </summary>
        public string EndpointAddress { get; set; }

        public void Load()
        {
        }

        public IEnumerable<string> ProduceSubKeys(IEnumerable<string> earlierKeys, string prefix, string delimiter)
        {
            return null;
        }

        public void Set(string key, string value)
        {
            throw new NotImplementedException();
        }

        public bool TryGet(string key, out string value)
        {
            switch (key)
            {
                case ConfigurationConstants.InstrumentationKeyFromConfig:
                case ConfigurationConstants.InstrumentationKeyForWebSites:
                    if (this.InstrumentationKey != null)
                    {
                        value = this.InstrumentationKey;
                        return true;
                    }
                    break;
                case ConfigurationConstants.DeveloperModeFromConfig:
                case ConfigurationConstants.DeveloperModeForWebSites:
                    if (this.DeveloperMode != null)
                    {
                        value = this.DeveloperMode.Value.ToString();
                        return true;
                    }
                    break;
                case ConfigurationConstants.EndpointAddressFromConfig:
                case ConfigurationConstants.EndpointAddressForWebSites:
                    if (this.EndpointAddress != null)
                    {
                        value = this.EndpointAddress;
                        return true;
                    }
                    break;
            }
            value = null;
            return false;
        }
    }
}
