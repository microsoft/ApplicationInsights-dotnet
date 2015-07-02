namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.TestFramework;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if WINRT
    using Windows.ApplicationModel;
    using Windows.Storage;
#endif

    [TestClass]
    public class PlatformTest
    {
        [TestCleanup]
        public void TestCleanup()
        {
            PlatformSingleton.Current = null;
        }

        [TestMethod]
        public void ClassIsPublicToAllowTestingOnWindowsRuntime()
        {
            Assert.IsFalse(typeof(PlatformSingleton).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void ClassIsStaticToServeOnlyAsSingletonFactory()
        {
            Assert.IsTrue(typeof(PlatformSingleton).GetTypeInfo().IsAbstract && typeof(PlatformSingleton).GetTypeInfo().IsSealed);
        }

        [TestMethod]
        public void CurrentIsAutomaticallyInitializedForEasyAccess()
        {
            IPlatform current = PlatformSingleton.Current;
            Assert.IsNotNull(current);
        }

        [TestMethod]
        public void CurrentCanBeSetToEnableMocking()
        {
            var platform = new StubPlatform();
            PlatformSingleton.Current = platform;
            Assert.AreSame(platform, PlatformSingleton.Current);
        }
        
#if !WINDOWS_STORE
        // TODO: Find a way to test Platform.ReadConfigurationXml in Windows 8.1 Store tests
        [TestMethod]
        public void ReadConfigurationXmlReturnsContentsOfApplicationInsightsConfigFileInApplicationInstallationDirectory()
        {
            CreateConfigurationFile("42");
            try
            {
                Assert.AreEqual("42", PlatformSingleton.Current.ReadConfigurationXml());
            }
            finally
            {
                DeleteConfigurationFile();
            }
        }
#endif

        [TestMethod]
        public void ReadConfigurationXmlIgnoresMissingApplicationInsightsConfigurationFileByReturningEmptyString()
        {
            string configuration = PlatformSingleton.Current.ReadConfigurationXml();
            Assert.IsNotNull(configuration);
            Assert.AreEqual(0, configuration.Length);
        }

        private static void CreateConfigurationFile(string content)
        {
            using (Stream fileStream = OpenConfigurationFile())
            {
                byte[] configurationBytes = Encoding.UTF8.GetBytes(content);
                fileStream.Write(configurationBytes, 0, configurationBytes.Length);
            }           
        }
        
        private static void DeleteConfigurationFile()
        {
#if WINRT
            StorageFile file = Package.Current.InstalledLocation.GetFileAsync("ApplicationInsights.config").GetAwaiter().GetResult();
            file.DeleteAsync().GetAwaiter().GetResult();
#elif WINDOWS_PHONE
            File.Delete(Path.Combine(Environment.CurrentDirectory, "ApplicationInsights.config"));
#else
            File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config"));
#endif
        }

        private static Stream OpenConfigurationFile()
        {
#if WINRT
            StorageFile file = Package.Current.InstalledLocation.CreateFileAsync("ApplicationInsights.config").GetAwaiter().GetResult();
            return file.OpenStreamForWriteAsync().GetAwaiter().GetResult();
#elif WINDOWS_PHONE
            return File.OpenWrite(Path.Combine(Environment.CurrentDirectory, "ApplicationInsights.config"));
#else
            return File.OpenWrite(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config"));
#endif
        }
    }
}
