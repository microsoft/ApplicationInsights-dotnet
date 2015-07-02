namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    internal class PlatformImplementation : IPlatform
    {
        public IDictionary<string, object> GetApplicationSettings()
        {
            return null;
        }

        public string ReadConfigurationXml()
        {
            return null;
        }

        public ExceptionDetails GetExceptionDetails(Exception exception, ExceptionDetails parentExceptionDetails)
        {
            return ExceptionConverter.ConvertToExceptionDetails(exception, parentExceptionDetails);
        }

        public IDebugOutput GetDebugOutput()
        {
            return new DebugOutput();
        }
    }
}
