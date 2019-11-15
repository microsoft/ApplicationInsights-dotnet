namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// PerformanceCollectorModuleTests tests.
    /// The goal is to test that the default list contains only those counters which are supported.
    /// Adding any unsupported counter by default will add noisy traces to user ikey.
    /// </summary>
    [TestClass]
    public class PerformanceCollectorModuleTests
    {        
        [TestMethod]
        public void PerformanceCollectorModuleDefaultContainsExpectedCountersNonWindows()
        {
#if NETCOREAPP2_0
            PerformanceCounterUtility.isAzureWebApp = null;
            var original = PerformanceCounterUtility.IsWindows;
            PerformanceCounterUtility.IsWindows = false;
            var module = new PerformanceCollectorModule();
            
            try
            {                                          
                module.Initialize(new TelemetryConfiguration());

                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\% Processor Time"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\% Processor Time Normalized"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\Private Bytes"));
                Assert.AreEqual(3, module.DefaultCounters.Count);
            }
            finally
            {
                PerformanceCounterUtility.IsWindows = original;
                module.Dispose();
            }
#endif
        }

        [TestMethod]
        public void PerformanceCollectorModuleDefaultContainsExpectedCountersWebApps()
        {
            PerformanceCounterUtility.isAzureWebApp = null;
            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "something");
            var module = new PerformanceCollectorModule();
            try
            {                                      
                module.Initialize(new TelemetryConfiguration());

                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\% Processor Time"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\% Processor Time Normalized"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\Private Bytes"));

                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Memory\Available Bytes"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\IO Data Bytes/sec"));

#if NET45
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests/Sec"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\.NET CLR Exceptions(??APP_CLR_PROC??)\# of Exceps Thrown / sec"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Execution Time"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests In Application Queue"));
                Assert.AreEqual(9, module.DefaultCounters.Count);
#else                
                Assert.AreEqual(5, module.DefaultCounters.Count);
#endif
            }
            finally
            {
                PerformanceCounterUtility.isAzureWebApp = null;
                module.Dispose();
                Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", string.Empty);
                Task.Delay(1000).Wait();
            }
        }

        [TestMethod]
        public void PerformanceCollectorModuleDefaultContainsExpectedCountersWindows()
        {
            PerformanceCounterUtility.isAzureWebApp = null;
            var module = new PerformanceCollectorModule();
#if NETCOREAPP2_0
            var original = PerformanceCounterUtility.IsWindows;
            PerformanceCounterUtility.IsWindows = true;
#endif
            try
            {                
                module.Initialize(new TelemetryConfiguration());
#if !NETCOREAPP1_0
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\% Processor Time"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\% Processor Time Normalized"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\Private Bytes"));

                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Memory\Available Bytes"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Process(??APP_WIN32_PROC??)\IO Data Bytes/sec"));

#if NET45
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests/Sec"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\.NET CLR Exceptions(??APP_CLR_PROC??)\# of Exceps Thrown / sec"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Execution Time"));
                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests In Application Queue"));
#endif

                Assert.IsTrue(ContainsPerfCounter(module.DefaultCounters, @"\Processor(_Total)\% Processor Time"));
#if NET45
                Assert.AreEqual(10, module.DefaultCounters.Count);
#else
                Assert.AreEqual(6, module.DefaultCounters.Count);
#endif

#endif

            }
            finally
            {
                module.Dispose();
#if NETCOREAPP2_0
            PerformanceCounterUtility.IsWindows = original;
#endif
            }
        }

        private bool ContainsPerfCounter(IList<PerformanceCounterCollectionRequest> counters, string name)
        {            
            foreach (var counter in counters)
            {
                if (counter.PerformanceCounter.Equals(name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}