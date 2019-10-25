// -----------------------------------------------------------------------
// <copyright file="ExceptionStatisticsTestBase.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// <summary></summary>
// -----------------------------------------------------------------------

namespace Functional.Helpers
{
    using System.Collections.Generic;
    using System.IO;

    public abstract class ExceptionStatisticsTestBase : SingleWebHostTestBase
    {
        private const string InstrumentationEngineProfilerModuleName = "MicrosoftInstrumentationEngine_x64.dll";
        private const string InstrumentationEngineProfilerId = 
            "{324F817A-7420-4E6D-B3C1-143FBED6D855}";

        private const string InstrumentationEngineApmcExtensionApmcId = 
            "{CA487940-57D2-10BF-11B2-A3AD5A13CBC0}";

        private const string InstrumentationEngineApmcExtensionApmcModuleName =
            "Microsoft.ApplicationInsights.ExtensionsHost_x64.dll";

        protected void AppendRtiaEnvironmentVariables(
            IDictionary<string, string> variables,
            string baseFolder)
        {
            variables.Add("COR_ENABLE_PROFILING", "1");
            variables.Add("COR_PROFILER", InstrumentationEngineProfilerId);
            variables.Add(
                "COR_PROFILER_PATH", 
                Path.Combine(baseFolder, InstrumentationEngineProfilerModuleName));
            variables.Add("MicrosoftInstrumentationEngine_Host", InstrumentationEngineApmcExtensionApmcId);
            variables.Add(
                "MicrosoftInstrumentationEngine_HostPath",
                    Path.Combine(baseFolder, InstrumentationEngineApmcExtensionApmcModuleName));
            variables.Add("MicrosoftInstrumentationEngine_FileLog", "Errors");
        }
    }
}
