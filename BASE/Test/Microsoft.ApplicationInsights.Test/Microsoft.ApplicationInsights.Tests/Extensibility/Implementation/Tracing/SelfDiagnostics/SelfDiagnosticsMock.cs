using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics;

namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    public class SelfDiagnosticsMock : ISelfDiagnostics
    {
        public string Level { get; set; }
        public string FileDirectory { get; set; }

        public void Initialize(string level, string fileDirectory)
        {
            this.Level = level;
            this.FileDirectory = fileDirectory;
        }
    }
}
