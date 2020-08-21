using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    internal interface ISelfDiagnostics
    {
        void Initialize(string level, string fileDirectory);
    }
}
