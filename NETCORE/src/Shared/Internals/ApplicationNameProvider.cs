namespace Microsoft.ApplicationInsights.Shared.Internals
{
    using System;
    using System.Reflection;

    /// <summary>
    /// This class provides the assembly name for the EventSource implementations.
    /// </summary>
    internal sealed class ApplicationNameProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationNameProvider"/> class.
        /// </summary>
        public ApplicationNameProvider()
        {
            this.Name = GetApplicationName();
        }

        /// <summary>
        /// Gets name of the current assembly.
        /// </summary>
        public string Name { get; private set; }

        private static string GetApplicationName()
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly();

                if (assembly == null)
                {
                    // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.getentryassembly?view=netcore-3.1#remarks
                    return "The GetEntryAssembly method can return null when a managed assembly has been loaded from an unmanaged application.";
                }
                else
                {
                    return assembly.GetName().Name;
                }
            }
            catch (Exception exp)
            {
                return "Undefined " + exp.Message;
            }
        }
    }
}
