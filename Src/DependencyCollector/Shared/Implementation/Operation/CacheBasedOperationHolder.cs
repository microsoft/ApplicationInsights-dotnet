namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation
{
    using System;
    using System.Globalization;
    using Microsoft.ApplicationInsights.DataContracts;

    internal sealed class CacheBasedOperationHolder
    {
        private readonly CacheProvider<Tuple<DependencyTelemetry, bool>> rddCallCache = new CacheProvider<Tuple<DependencyTelemetry, bool>>(100 * 1000);        

        public Tuple<DependencyTelemetry, bool> Get(long id)
        {
            var telemetryTuple = this.rddCallCache.Get(id.ToString(CultureInfo.InvariantCulture));
            return telemetryTuple;
        }

        public bool Remove(long id)
        {
            return this.rddCallCache.Remove(id.ToString(CultureInfo.InvariantCulture));
        }

        public void Store(long id, Tuple<DependencyTelemetry, bool> telemetryTuple)
        {
            if (telemetryTuple == null)
            {
                throw new ArgumentNullException("telemetryTuple");
            }

            // it might be possible to optimize by preventing the long to string conversion
            this.rddCallCache.Set(id.ToString(CultureInfo.InvariantCulture), telemetryTuple);
        }       
    }
}
