//-----------------------------------------------------------------------
// <copyright file="TelemetryLoggerScope.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.ApplicationInsights.AspNet.Extensibility.Implementation.Tracing
{
    using System;

    /// <summary>
    /// Logging scope for <code>TelemetryLogger.BeginScopeImpl</code> calls.
    /// </summary>
    public class TelemetryLoggerScope : IDisposable
    {
        /// <summary>State of the logging scope instance.</summary>
        private bool isDisposed = false;

        /// <summary>
        /// Gets a value indicating whether or not the logging scope instance is disposed.
        /// </summary>
        internal bool IsDisposed
        {
            get
            {
                return this.isDisposed;
            }
        }

        /// <summary>
        /// Disposes of this instance.
        /// </summary>
        public void Dispose()
        {
            this.isDisposed = true;
        }
    }
}
