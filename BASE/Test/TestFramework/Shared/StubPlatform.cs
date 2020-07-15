namespace Microsoft.ApplicationInsights.TestFramework
{
    using System;
    using System.IO;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class StubPlatform : IPlatform
    {
        public Func<IDebugOutput> OnGetDebugOutput = () => new StubDebugOutput();
        public Func<string> OnReadConfigurationXml = () => null;
        public Func<string> OnGetMachineName = () => null;

        public string ReadConfigurationXml()
        {
            return this.OnReadConfigurationXml();
        }

        public IDebugOutput GetDebugOutput()
        {
            return this.OnGetDebugOutput();
        }

        public virtual bool TryGetEnvironmentVariable(string name, out string value)
        {
            value = string.Empty;

            try
            {
                value = Environment.GetEnvironmentVariable(name);
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception e)
            {
                CoreEventSource.Log.FailedToLoadEnvironmentVariables(e.ToString());
            }

            return false;
        }

        public string GetMachineName()
        {
            return this.OnGetMachineName();
        }

        public void TestDirectoryPermissions(DirectoryInfo directory)
        {
            string testFileName = Path.GetRandomFileName();
            string testFilePath = Path.Combine(directory.FullName, testFileName);

            if (!Directory.Exists(directory.FullName))
            {
                Directory.CreateDirectory(directory.FullName);
            }

            // Create a test file, will auto-delete this file. 
            using (var testFile = new FileStream(testFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose))
            {
                testFile.Write(new[] { default(byte) }, 0, 1);
            }
        }

        public string GetCurrentIdentityName() => nameof(StubPlatform);
    }
}
