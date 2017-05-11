//-----------------------------------------------------------------------
// <copyright file="DiagnosticSourceListenerSubscription.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.DiagnosticSourceListener
{
    using Microsoft.ApplicationInsights.DataContracts;
    using System;
    using System.Collections.Generic;

    internal class DiagnosticSourceListenerSubscription : IObserver<KeyValuePair<string, object>>
    {
        private readonly string listenerName;
        private readonly TelemetryClient telemetryClient;

        public DiagnosticSourceListenerSubscription(string listenerName, TelemetryClient telemetryClient)
        {
            if (listenerName == null)
            {
                throw new ArgumentNullException(nameof(listenerName));
            }

            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            this.listenerName = listenerName;
            this.telemetryClient = telemetryClient;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// Observes an event from the diagnostic source and logs it as a message to Application Insights.
        /// </summary>
        /// <param name="event">The event (message and payload) from the diagnostic source.</param>
        public void OnNext(KeyValuePair<string, object> @event)
        {
            var message = @event.Key;
            var payload = @event.Value;

            var telemetry = new TraceTelemetry(message, SeverityLevel.Information);
            telemetry.Properties.Add("DiagnosticSource", this.listenerName);

            // Transfer properties from payload to telemetry
            if (payload != null)
            {
                foreach (var property in DeclaredPropertiesCache.GetDeclaredProperties(payload))
                {
                    if (!property.IsSpecialName)
                    {
                        telemetry.Properties.Add(property.Name, property.GetValue(payload).ToString());
                    }
                }
            }

            this.telemetryClient.TrackTrace(telemetry);
        }
    }
}
