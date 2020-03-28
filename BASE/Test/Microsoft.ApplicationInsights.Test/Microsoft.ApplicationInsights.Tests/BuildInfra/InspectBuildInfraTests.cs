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
            string frameworkDirectoryName = null;

#if NET45
            frameworkDirectoryName = "net45";
#elif NET46
            frameworkDirectoryName = "net46";
#elif NETCOREAPP1_1
            frameworkDirectoryName = "netcoreapp1.1";
#elif NETCOREAPP2_0
            frameworkDirectoryName = "netcoreapp2.0";
#elif NETCOREAPP3_0
            frameworkDirectoryName = "netcoreapp3.0";
#else
            throw new Exception("unconfigured test");
#endif

            var testAssembly = GetTestAssembly();
            var sdkAssembly = GetBaseSdkAssembly();
            PrintInfoToConsole(testAssembly: testAssembly, sdkAssembly: sdkAssembly);

            Assert.IsTrue(testAssembly.Location.Contains(frameworkDirectoryName));
            Assert.IsTrue(sdkAssembly.Location.Contains(frameworkDirectoryName));
        }

        private Assembly GetBaseSdkAssembly()
        {
#if NETCOREAPP1_1
            return typeof(TelemetryClient).GetTypeInfo().Assembly;
#else
            return AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name == "Microsoft.ApplicationInsights");
#endif
        }

        private Assembly GetTestAssembly()
        {

#if NETCOREAPP1_1
            return typeof(InspectBuildInfraTests).GetTypeInfo().Assembly;
#else
            return Assembly.GetExecutingAssembly();
#endif
        }


        [TestMethod]
        public void VerifyCorrectAssemblyDirectories()
        {
            var testAssembly = GetTestAssembly();
            var sdkAssembly = GetBaseSdkAssembly();
            PrintInfoToConsole(testAssembly: testAssembly, sdkAssembly: sdkAssembly);


            var testDirectoryInfo = new DirectoryInfo(testAssembly.Location);
            var testDirectory = testDirectoryInfo.Parent;

            var sdkDirectoryInfo = new DirectoryInfo(sdkAssembly.Location);
            var sdkDirectory = sdkDirectoryInfo.Parent;

            Assert.AreEqual(testDirectory.FullName, sdkDirectory.FullName);
        }


        private void PrintInfoToConsole(Assembly testAssembly, Assembly sdkAssembly)
        {
            Console.WriteLine($"Test Assembly: {testAssembly.Location}");
            Console.WriteLine($"SDK Assembly: {sdkAssembly.Location}");

            var sdkVersion = sdkAssembly.GetName().Version.ToString();
            Console.WriteLine($"SDK Version: {sdkVersion}");
        }
    }
}
