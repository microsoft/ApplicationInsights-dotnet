namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Microsoft.ApplicationInsights.DataContracts;

    internal class ObjectInstanceBasedOperationHolder
    {
        private ConditionalWeakTable<object, Tuple<DependencyTelemetry, bool>> weakTableForCorrelation = new ConditionalWeakTable<object, Tuple<DependencyTelemetry, bool>>();

        public Tuple<DependencyTelemetry, bool> Get(object holderInstance)
        {
            if (holderInstance == null)
            {
                throw new ArgumentNullException(nameof(holderInstance));
            }

            Tuple<DependencyTelemetry, bool> result = null;
            if (!this.weakTableForCorrelation.TryGetValue(holderInstance, out result))
            {
                result = null;
            }

            return result;
        }

        public bool Remove(object holderInstance)
        {
            if (holderInstance == null)
            {
                throw new ArgumentNullException(nameof(holderInstance));
            }

            return this.weakTableForCorrelation.Remove(holderInstance);
        }

        public void Store(object holderInstance, Tuple<DependencyTelemetry, bool> telemetryTuple)
        {
            if (holderInstance == null)
            {
                throw new ArgumentNullException(nameof(holderInstance));
            }

            if (telemetryTuple == null)
            {
                throw new ArgumentNullException(nameof(telemetryTuple));
            }

            this.weakTableForCorrelation.Add(holderInstance, telemetryTuple);
        }
    }
}
