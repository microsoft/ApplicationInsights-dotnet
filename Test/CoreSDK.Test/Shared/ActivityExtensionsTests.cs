#if !NET40
namespace Microsoft.ApplicationInsights
{
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    using System.Reflection;

    [TestClass]
    public class ActivityExtensionsTests
    {
        [TestMethod]
        public void CanLoadDiagnosticSourceAssembly()
        {
            // verifies that currently referenced DiagnosticSource version, culture and public token matches one checked in ActivityExtension.Initialize
            // if fails, fix the ActivityExtension as well
            Assert.DoesNotThrow(() => 
                Assembly.Load(new AssemblyName("System.Diagnostics.DiagnosticSource, Version=4.0.2.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51")));
        }
    }
}
#endif