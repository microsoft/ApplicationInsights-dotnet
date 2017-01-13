namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Threading;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;

    internal class DependencyTableStore : IDisposable
    {
        internal CacheBasedOperationHolder WebRequestCacheHolder;
        internal CacheBasedOperationHolder SqlRequestCacheHolder;
        internal ObjectInstanceBasedOperationHolder WebRequestConditionalHolder;
        internal ObjectInstanceBasedOperationHolder SqlRequestConditionalHolder;

        internal bool IsProfilerActivated = false;
        private static DependencyTableStore instance;

        private DependencyTableStore() 
        {
#if !NET40
            this.WebRequestCacheHolder = new CacheBasedOperationHolder("aisdkwebrequests", 100 * 1000);
            this.SqlRequestCacheHolder = new CacheBasedOperationHolder("aisdksqlrequests", 100 * 1000);
#endif
            this.WebRequestConditionalHolder = new ObjectInstanceBasedOperationHolder();
            this.SqlRequestConditionalHolder = new ObjectInstanceBasedOperationHolder();
        }

        internal static DependencyTableStore Instance
        {
           get 
           {
              Interlocked.CompareExchange<DependencyTableStore>(ref instance, new DependencyTableStore(), null);
              return instance;
           }
        }

        public void Dispose()
        {            
            this.WebRequestCacheHolder.Dispose();
            this.SqlRequestCacheHolder.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
