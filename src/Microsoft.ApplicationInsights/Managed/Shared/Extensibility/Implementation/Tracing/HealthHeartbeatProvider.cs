namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Implementation of health heartbeat functionality.
    /// </summary>
    internal class HealthHeartbeatProvider : IDisposable, IHeartbeatProvider
    {
        /// <summary>
        /// The default interval between heartbeats if not specified by the user
        /// </summary>
        public static int DefaultHeartbeatIntervalMs = 5000;

        /// <summary>
        /// The default fields to include in every heartbeat sent. Note that setting the value to '*' includes all default fields.
        /// </summary>
        public static string DefaultAllowedFieldsInHeartbeatPayload = "*";

        private bool disposedValue = false; // To detect redundant calls to dispose
        private int intervalBetweenHeartbeatsMs; // time between heartbeats emitted specified in milliseconds
        private string enabledHeartbeatPayloadFields; // string containing fields that are enabled in the payload. * means everything available.

        public HealthHeartbeatProvider() : this(DefaultHeartbeatIntervalMs, DefaultAllowedFieldsInHeartbeatPayload)
        {
        }

        public HealthHeartbeatProvider(int delayMs) : this(delayMs, DefaultAllowedFieldsInHeartbeatPayload)
        {
        }

        public HealthHeartbeatProvider(string allowedPayloadFields) : this(DefaultHeartbeatIntervalMs, allowedPayloadFields)
        {
        }

        public HealthHeartbeatProvider(int delayMs, string allowedPayloadFields)
        {
            this.enabledHeartbeatPayloadFields = allowedPayloadFields;
            this.intervalBetweenHeartbeatsMs = delayMs;
        }

        public int HeartbeatIntervalMs => this.intervalBetweenHeartbeatsMs;

        public string EnabledPayloadFields => this.enabledHeartbeatPayloadFields;

        public bool Initialize()
        {
            return true;
        }

        public void RegisterHeartbeatPayload(IHealthHeartbeatPayloadExtension payloadProvider)
        {
            if (payloadProvider == null)
            {
                throw new ArgumentNullException(nameof(payloadProvider));
            }

            throw new NotImplementedException();
        }

        public bool UpdateSettings()
        {
            return true;
        }

        #region IDisposable Support

        // Override the finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HealthHeartbeatProvider() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                this.disposedValue = true;
            }
        }

        #endregion
    }
}
