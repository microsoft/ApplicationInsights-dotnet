namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System.Collections.Generic;

    /// <summary>
    /// Component Correlation configuration.
    /// </summary>
    public class ComponentCorrelation
    {
        private ICollection<string> excludedDomains = new SanitizedHostList();

        /// <summary>
        /// Gets or sets a value indicating whether component correlation collection is enabled. That is should there be an attempt to add component correlation related headers to outgoing responses.
        /// </summary>
        public bool CollectionEnabled { get; set; } = true;

        /// <summary>
        /// Gets the list object that is meant to hold domains to exclude.
        /// </summary>
        public ICollection<string> ExcludedDomains
        {
            get
            {
                return this.excludedDomains;
            }
        }
    }
}