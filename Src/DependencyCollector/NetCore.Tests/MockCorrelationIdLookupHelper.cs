namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using Microsoft.ApplicationInsights.Common;
    using System.Collections.Generic;

    internal class MockCorrelationIdLookupHelper : ICorrelationIdLookupHelper
    {
        private readonly Dictionary<string, string> instrumentationKeyToCorrelationIdMap;
        
        public MockCorrelationIdLookupHelper(Dictionary<string, string> instrumentationKeyToCorrelationIdMap)
        {
            this.instrumentationKeyToCorrelationIdMap = instrumentationKeyToCorrelationIdMap;
        }

        public bool TryGetXComponentCorrelationId(string instrumentationKey, out string correlationId)
        {
            return this.instrumentationKeyToCorrelationIdMap.TryGetValue(instrumentationKey, out correlationId);
        }
    }
}
