//-----------------------------------------------------------------------
// <copyright file="EtwListeningRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.EtwCollector
{
    using System;
    using Microsoft.Diagnostics.Tracing;

    public class EtwListeningRequest
    {
        public string ProviderName { get; set; }

        public Guid ProviderGuid { get; set; }

        public TraceEventLevel Level { get; set; }

        public ulong Keywords { get; set; }

        /// <summary>
        /// Validate the currnet etw listening request is valid.
        /// </summary>
        /// <exception cref="ArgumentNullException">Throws when the object is not valid.</exception>
        public void Validate()
        {
            if (this.ProviderGuid == Guid.Empty && string.IsNullOrEmpty(this.ProviderName))
            {
                throw new ArgumentException("ProviderGuid and ProviderName can't be null at the same time.");
            }
        }
    }
}
