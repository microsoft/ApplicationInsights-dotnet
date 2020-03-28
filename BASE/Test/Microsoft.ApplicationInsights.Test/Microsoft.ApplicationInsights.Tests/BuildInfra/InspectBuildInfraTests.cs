using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights.TestFramework.BuildInfra
{
    [TestClass]
    public class InspectBuildInfraTests
    {
        [TestMethod]
        public void VerifyAssemblyDirectoryContainsFrameworkName()
        {
            var frameworkDirectoryName = GetExpectedFrameworkDirectoryName();
            var testAssembly = GetTestAssembly();

            Assert.IsTrue(testAssembly.Location.Contains(frameworkDirectoryName));
        }

        [TestMethod]
        public void VerifyCorrectAssemblyDirectories()
        {
            var testAssembly = GetTestAssembly();
            var sdkAssembly = GetBaseSdkAssembly();


            var testDirectoryInfo = new DirectoryInfo(testAssembly.Location);
            var testDirectory = testDirectoryInfo.Parent;

            var sdkDirectoryInfo = new DirectoryInfo(sdkAssembly.Location);
            var sdkDirectory = sdkDirectoryInfo.Parent;

            Assert.AreEqual(testDirectory.FullName, sdkDirectory.FullName);
        }

        private string GetExpectedFrameworkDirectoryName()
        {
#if NET45
            return "net45";
#elif NET46
            return "net46";
#elif NETCOREAPP1_1
            return "netcoreapp1.1";
#elif NETCOREAPP2_0
            return "netcoreapp2.0";
#elif NETCOREAPP3_0
            return "netcoreapp3.0";
#else
            throw new Exception("unconfigured test");
#endif
        }

        private Assembly GetBaseSdkAssembly()
        {
#if NETCOREAPP1_1
            Assembly assembly = typeof(TelemetryClient).GetTypeInfo().Assembly;
#else
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name == "Microsoft.ApplicationInsights");
#endif

            Console.WriteLine($"SDK Assembly: {assembly.Location}");
            var sdkVersion = assembly.GetName().Version.ToString();
            Console.WriteLine($"SDK Version: {sdkVersion}");

            return assembly;
        }

        private Assembly GetTestAssembly()
        {

#if NETCOREAPP1_1
            Assembly assembly = typeof(InspectBuildInfraTests).GetTypeInfo().Assembly;
#else
            Assembly assembly = Assembly.GetExecutingAssembly();
#endif

            Console.WriteLine($"Test Assembly: {assembly.Location}");

            return assembly;
        }
    }
}
