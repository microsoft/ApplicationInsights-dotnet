namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;

    internal sealed class ApplicationNameProvider
    {
        public ApplicationNameProvider()
        {
            this.Name = this.GetApplicationName();
        }

        public string Name { get; private set; }

        private string GetApplicationName()
        {
            //// We want to add application name to all events BUT
            //// It is prohibited by EventSource rules to have more parameters in WriteEvent that in event source method
            //// Parameter will be available in payload but in the next versions EventSource may 
            //// start validating that number of parameters match
            //// It is not allowed to call additional methods, only WriteEvent

            string name;
            try
            {
#if !WINRT && !CORE_PCL && !UWP
                name = AppDomain.CurrentDomain.FriendlyName;
#else
                name = string.Empty;
#endif
            }
            catch (Exception exp)
            {
                name = "Undefined " + exp.Message ?? exp.ToString();
            }

            return name;
        }
    }
}
