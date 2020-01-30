// <copyright file="IAsyncFlushable.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a communication channel for sending telemetry to application insights.
    /// </summary>
    public interface IAsyncFlushable : IDisposable
    {
        /// <summary>
        /// Flushes the in-memory buffer asynchronously.
        /// </summary>
        /// <returns>The task to await.</returns>
        Task<bool> FlushAsync(CancellationToken cancellationToken);
    }
}
