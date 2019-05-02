namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;

    internal sealed class ApplicationNameProvider
    {
        public ApplicationNameProvider()
        {
            //// We want to add application name to all events BUT
            //// It is prohibited by EventSource rules to have more parameters in WriteEvent that in event source method
            //// Parameter will be available in payload but in the next versions EventSource may 
            //// start validating that number of parameters match
            //// It is not allowed to call additional methods, only WriteEvent

            try
            {
#if !NETSTANDARD1_3
                this.Name = AppDomain.CurrentDomain.FriendlyName + " 1";
#else
                this.Name = string.Empty + "123";
#endif
            }
            catch (Exception exp)
            {
                this.Name = "Undefined " + exp.Message ?? exp.ToString();
            }
        }

        public string Name { get; private set; }
    }
}
