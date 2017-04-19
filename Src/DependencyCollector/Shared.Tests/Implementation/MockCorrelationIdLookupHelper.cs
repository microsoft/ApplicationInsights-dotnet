namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Common;

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
