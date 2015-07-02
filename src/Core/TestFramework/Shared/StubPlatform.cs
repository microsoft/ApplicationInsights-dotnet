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

        public IDictionary<string, object> GetApplicationSettings()
        {
            return this.OnGetApplicationSettings();
        }

        public string ReadConfigurationXml()
        {
            return this.OnReadConfigurationXml();
        }

        public ExceptionDetails GetExceptionDetails(Exception exception, ExceptionDetails parentExceptionDetails)
        {
            return this.OnGetExceptionDetails(exception, parentExceptionDetails);
        }

        public IDebugOutput GetDebugOutput()
        {
            return this.OnGetDebugOutput();
        }
    }
}
