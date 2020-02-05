// <copyright file="IAsyncFlushable.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents asynchronous flush for  to application insights.
    /// </summary>
    public interface IAsyncFlushable : IDisposable
    {
        /// <summary>
        /// Flushes the in-memory buffer asynchronously.
        /// </summary>
        /// <returns>
        /// True indicates telemetry data ownership is transferred out of process, that are emitted before the flush invocation.
        /// False indicates transfer of telemetry data has failed, the process still owns all or part of the telemetry.
        /// </returns>
        Task<bool> FlushAsync(CancellationToken cancellationToken);
    }
}
