namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a request to filter out a custom user agent string.
    /// </summary>
    public class FilterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRequest"/> class.
        /// </summary>
        public FilterRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRequest"/> class.
        /// </summary>
        /// <param name="value">User agent string to apply the filter to.</param>
        public FilterRequest(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the user agent string to apply the filter to.
        /// </summary>
        public string Value { get; set; }
    }
}
