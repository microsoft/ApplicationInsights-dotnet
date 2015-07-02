namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.IO;
    using System.Text;
#if WINDOWS_PHONE_APP || WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using Windows.ApplicationModel;
    using Windows.Storage;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

    /// <summary>
    /// Shared, platform-neutral tests for <see cref="PlatformImplementation"/> class.
    /// </summary>
    [TestClass]
    public partial class PlatformImplementationTest : IDisposable
    {
        public PlatformImplementationTest()
        {
            // Make sure configuration files created by other tests don't brake these.
            DeleteConfigurationFile();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

#if !WINDOWS_STORE
        // TODO: Find a way to test ReadConfigurationXml on Windows 8.1
        [TestMethod]
        public void ReadConfigurationXmlReturnsContentsOfApplicationInsightsConfigFileInApplicationInstallationDirectory()
        {
            const string TestFileContent = "42";
            CreateConfigurationFile(TestFileContent);
            var platform = new PlatformImplementation();

            string s = platform.ReadConfigurationXml();
            
            Assert.AreEqual(TestFileContent, s);
        }
#endif

        [TestMethod]
        public void ReadConfigurationXmlIgnoresMissingApplicationInsightsConfigurationFileByReturningEmptyString()
        {
            var platform = new PlatformImplementation();

            string configuration = platform.ReadConfigurationXml();
            
            Assert.AreEqual(0, configuration.Length);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing == true)
            {
                DeleteConfigurationFile();
            }
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
            try 
            {
                StorageFile file = Package.Current.InstalledLocation.GetFileAsync("ApplicationInsights.config").GetAwaiter().GetResult();
                file.DeleteAsync().GetAwaiter().GetResult();
            }
            catch (FileNotFoundException)
            {
            }
#elif WINDOWS_PHONE_APP
            File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config"));
#else
            File.Delete(Path.Combine(Environment.CurrentDirectory, "ApplicationInsights.config"));
#endif
        }

        private static Stream OpenConfigurationFile()
        {
#if WINRT
            StorageFile file = Package.Current.InstalledLocation.CreateFileAsync("ApplicationInsights.config", CreationCollisionOption.ReplaceExisting).GetAwaiter().GetResult();
            return file.OpenStreamForWriteAsync().GetAwaiter().GetResult();
#elif WINDOWS_PHONE_APP
            return File.OpenWrite(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config"));
#else
            return File.OpenWrite(Path.Combine(Environment.CurrentDirectory, "ApplicationInsights.config"));
#endif
        }
    }
}
