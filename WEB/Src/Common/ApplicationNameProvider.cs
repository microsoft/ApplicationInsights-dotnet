namespace Microsoft.ApplicationInsights.Common
{
    using System;
#if NETSTANDARD1_6
    using System.Reflection;
#endif

    internal sealed class ApplicationNameProvider
    {
        public ApplicationNameProvider()
        {
            this.Name = GetApplicationName();
        }

        public string Name { get; private set; }

        private static string GetApplicationName()
        {
            string name;
            try
            {
#if NETSTANDARD1_6
                name = Assembly.GetEntryAssembly().FullName;
#else
                name = AppDomain.CurrentDomain.FriendlyName;
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
