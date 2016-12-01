namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Component Correlation configuration.
    /// </summary>
    public class ComponentCorrelation
    {
        /// <summary>
        /// Gets or sets a value indicating whether correlation collection is enabled. That is should there be an attempt to add component correlation related headers to outgoing requests.
        /// </summary>
        public bool CollectionEnabled { get; set; } = true;
    }
}