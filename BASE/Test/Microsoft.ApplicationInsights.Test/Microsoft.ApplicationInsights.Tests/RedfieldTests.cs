// #define REDFIELD // can use this to enable IntelliSense
#if REDFIELD

namespace Microsoft.ApplicationInsights
{
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// These tests are a sanitiy check.
    /// This will serve as a verification that the Redfield compilation flag worked as expected.
    /// Devs should review test logs and confirm that these tests run and pass.
    /// 
    /// To run these tests locally:
    /// dotnet build /p:Redfield=True ".\dotnet\BASE\Microsoft.ApplicationInsights.sln"
    /// dotnet test ".\bin\Debug\test\Microsoft.ApplicationInsights.Tests\net6.0\Microsoft.ApplicationInsights.Tests.dll" --filter ClassName~"Microsoft.ApplicationInsights.RedfieldTests"
    /// dotnet test ".\bin\Debug\test\Microsoft.ApplicationInsights.Tests\net6.0\Microsoft.ApplicationInsights.Tests.dll" --filter Name~VerifyRedfieldEventSourceName
    /// </summary>
    [TestClass]
    public class RedfieldTests
    {
        /// <summary>
        /// The 'Redfield' compilation flag should switch the name of EventSource class.
        /// 
        /// To run this tests locally:
        /// dotnet test ".\bin\Debug\test\Microsoft.ApplicationInsights.Tests\net6.0\Microsoft.ApplicationInsights.Tests.dll" --filter Name~VerifyRedfieldEventSourceName
        /// </summary>
        [TestMethod]
        public void VerifyRedfieldEventSourceName()
        {
            var expectedName = "Redfield-Microsoft-ApplicationInsights-Core";

            var eventSourceAttribute = typeof(CoreEventSource)
                .GetCustomAttributes(attributeType: typeof(EventSourceAttribute), inherit: false)
                .Single() as EventSourceAttribute;

            Assert.IsNotNull(eventSourceAttribute);
            Assert.AreEqual(expectedName, eventSourceAttribute.Name);
        }

        /// <summary>
        /// Redfield takes a dependency on a different version of System.Diagnostics.DiagnosticSource. Package version: "4.7.0.0". Assembly version: "4.0.5.0".
        /// 
        /// To run this tests locally:
        /// dotnet test ".\bin\Debug\test\Microsoft.ApplicationInsights.Tests\net6.0\Microsoft.ApplicationInsights.Tests.dll" --filter Name~VerifyRedfieldDiagnosticSourceVersion
        /// </summary>
        [TestMethod]
        public void VerifyRedfieldDiagnosticSourceVersion()
        {
            var referencedAssemblies = typeof(TelemetryClient).Assembly.GetReferencedAssemblies();
            var diagnosticSource = referencedAssemblies.Single(x => x.Name == "System.Diagnostics.DiagnosticSource");
            Assert.AreEqual("4.0.5.0", diagnosticSource.Version.ToString());
        }
    }
}
#endif
