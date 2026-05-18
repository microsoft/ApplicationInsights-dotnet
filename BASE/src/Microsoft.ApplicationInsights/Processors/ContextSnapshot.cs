namespace Microsoft.ApplicationInsights.Processors
{
    using System;
    using System.Collections.Generic;

    internal sealed class ContextSnapshot
    {
        internal ContextSnapshot(KeyValuePair<string, string>[] tags)
        {
            this.Tags = tags ?? Array.Empty<KeyValuePair<string, string>>();
        }

        internal KeyValuePair<string, string>[] Tags { get; }
    }
}
