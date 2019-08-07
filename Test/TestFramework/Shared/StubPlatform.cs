namespace Microsoft.ApplicationInsights.TestFramework
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

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

        public string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        public bool TryGetEnvironmentVariable(string name, out string value)
        {
            value = Environment.GetEnvironmentVariable(name);
            return !string.IsNullOrEmpty(value);
        }

        public string GetMachineName()
        {
            return this.OnGetMachineName();
        }
    }
}
