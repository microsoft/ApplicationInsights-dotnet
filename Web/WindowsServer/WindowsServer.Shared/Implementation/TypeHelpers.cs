namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
    using System.Linq;
    
    internal static class TypeHelpers
    {
        /// <summary>
        /// Gets the type by type name from the assembly.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="assemblyName">The assembly name.</param>
        /// <returns>Return type from assembly loaded in the process by assembly and type name.</returns>
        public static Type GetLoadedType(string typeName, string assemblyName)
        {
            // This method is different from Type.GetType because GetType returns null if you do not specify fully qualifed assembly name
            // Type.GetType would work only for mscorlib or current executing assembly
            // If assembly is not loaded yet this method will return null
            Type type = null;

            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(item => string.Equals(item.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));

            if (assembly != null)
            {
                type = assembly.GetType(typeName, false);

                if (type == null)
                {
                    WindowsServerEventSource.Log.TypeExtensionsTypeNotLoaded(typeName);
                }
            }
            else
            {
                WindowsServerEventSource.Log.TypeExtensionsAssemblyNotLoaded(assemblyName);
            }

            return type;
        }
    }
}
