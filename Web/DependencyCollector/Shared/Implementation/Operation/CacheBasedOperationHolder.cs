namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ApplicationInsights.DataContracts;

    internal sealed class CacheBasedOperationHolder : IDisposable
    {
        private readonly CacheProvider<Tuple<DependencyTelemetry, bool>> rddCallCache = new CacheProvider<Tuple<DependencyTelemetry, bool>>(100 * 1000);

        public Tuple<DependencyTelemetry, bool> Get(long id)
        {
            var telemetryTuple = this.rddCallCache.Get(id);
            return telemetryTuple;
        }

        public bool Remove(long id)
        {
            return this.rddCallCache.Remove(id);
        }

        public void Store(long id, Tuple<DependencyTelemetry, bool> telemetryTuple)
        {
            if (telemetryTuple == null)
            {
                throw new ArgumentNullException("telemetryTuple");
            }

            this.rddCallCache.Set(id, telemetryTuple);
        }

        public void Dispose()
        {
            this.rddCallCache.Dispose();
        }
    }
}
