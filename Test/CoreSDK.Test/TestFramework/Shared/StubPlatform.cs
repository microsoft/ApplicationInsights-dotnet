namespace Microsoft.ApplicationInsights.TestFramework
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    internal class StubPlatform : IPlatform
    {
        public Func<IDictionary<string, object>> OnGetApplicationSettings = () => new Dictionary<string, object>();
        public Func<IDebugOutput> OnGetDebugOutput = () => new StubDebugOutput();
        public Func<string> OnReadConfigurationXml = () => null;
        public Func<Exception, ExceptionDetails, ExceptionDetails> OnGetExceptionDetails = (e, p) => new ExceptionDetails();

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
    }
}
