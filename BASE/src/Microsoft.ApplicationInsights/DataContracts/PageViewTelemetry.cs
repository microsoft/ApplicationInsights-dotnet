namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Telemetry type used to track page views.
    /// </summary>
    /// <remarks>
    /// You can send information about pages viewed by your application to Application Insights by
    /// passing an instance of the <see cref="PageViewTelemetry"/> class to the <see cref="TelemetryClient.TrackPageView(PageViewTelemetry)"/>
    /// method.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#page-views">Learn more</a>
    /// </remarks>
    internal sealed class PageViewTelemetry : ITelemetry, ISupportProperties
    {
        internal const string EtwEnvelopeName = "PageView";
        internal string EnvelopeName = "AppPageViews";
        private readonly TelemetryContext context;

        private double? samplingPercentage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageViewTelemetry"/> class.
        /// </summary>
        public PageViewTelemetry()
        {
            this.context = new TelemetryContext();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageViewTelemetry"/> class with the
        /// specified <paramref name="pageName"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The <paramref name="pageName"/> is null or empty string.</exception>
        public PageViewTelemetry(string pageName) : this()
        {
            this.Name = pageName;
            this.context = new TelemetryContext();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageViewTelemetry"/> class by cloning an existing instance.
        /// </summary>
        /// <param name="source">Source instance of <see cref="PageViewTelemetry"/> to clone from.</param>
        private PageViewTelemetry(PageViewTelemetry source)
        {
            this.Timestamp = source.Timestamp;
            this.samplingPercentage = source.samplingPercentage;
            this.context = new TelemetryContext();
            // this.ProactiveSamplingDecision = source.ProactiveSamplingDecision;
        }

        /// <summary>
        /// Gets or sets date and time when event was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public string Sequence { get; set; }

        /// <summary>
        /// Gets the context associated with the current telemetry item.
        /// </summary>
        public TelemetryContext Context
        {
            get { return this.context; }
        }

        /// <summary>
        /// Gets or sets page view ID.
        /// </summary>
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the page.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the page view Uri.
        /// </summary>
        public Uri Url
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the page view duration.
        /// </summary>
        public TimeSpan Duration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a dictionary of custom defined metrics.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, double> Metrics
        {
            get;
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this page view.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get;
        }

        // <summary>`
        // Gets or sets data sampling percentage (between 0 and 100).
        // Should be 100/n where n is an integer. <a href="https://go.microsoft.com/fwlink/?linkid=832969">Learn more</a>
        // </summary>
        /*double? ISupportSampling.SamplingPercentage
        {
            get { return this.samplingPercentage; }
            set { this.samplingPercentage = value; }
        }*/

        // <summary>
        // Gets item type for sampling evaluation.
        // </summary>
        // public SamplingTelemetryItemTypes ItemTypeFlag => SamplingTelemetryItemTypes.PageView;

        /// <summary>
        /// Deeply clones a <see cref="PageViewTelemetry"/> object.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        public ITelemetry DeepClone()
        {
            return new PageViewTelemetry(this);
        }
    }
}
