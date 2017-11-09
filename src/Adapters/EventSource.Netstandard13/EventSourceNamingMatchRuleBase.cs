//-----------------------------------------------------------------------
// <copyright file="EventSourceNamingMatchRuleBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace Microsoft.ApplicationInsights.EventSourceListener
{
    /// <summary>
    /// Abstraction of a match rule for event source. To be inherited by either an enabling rule or a disabling rule, etc.
    /// </summary>
    public abstract class EventSourceNamingMatchRuleBase
    {
        /// <summary>
        /// Gets or sets the name of the EventSource to listen to.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether allows wildcards in <see cref="Name" />.
        /// </summary>
        public bool PrefixMatch { get; set; }
    }
}
