namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Partial class to add the EventData attribute and any additional customizations to the generated type.
    /// </summary>
#if NET40
    [Microsoft.Diagnostics.Tracing.EventData(Name = "PartB_PageViewData")]
#elif !NET45
    // .Net 4.5 has a custom implementation of RichPayloadEventSource
    [System.Diagnostics.Tracing.EventData(Name = "PartB_PageViewData")]
#endif
    internal partial class PageViewData : IDeepCloneable<PageViewData>
    {
        PageViewData IDeepCloneable<PageViewData>.DeepClone()
        {
            var other = new PageViewData();
            this.ApplyProperties(other);
            return other;
        }

        protected override void ApplyProperties(EventData other)
        {
            base.ApplyProperties(other);
            PageViewData otherPageView = other as PageViewData;
            if (otherPageView != null)
            {
                otherPageView.url = this.url;
                otherPageView.duration = this.duration;
            }
        }
    }
}