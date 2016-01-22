namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    /// <summary>
    /// The wrapper for the Azure Service Runtime.
    /// </summary>
    internal class ServiceRuntime
    {
        /// <summary>
        /// Gets the role environment.
        /// </summary>
        /// <param name="baseDirectory">The base directory.</param>
        /// <returns>
        /// The role environment object.
        /// </returns>
        public RoleEnvironment GetRoleEnvironment(string baseDirectory = null)
        {
            // TODO: remove factory
            return new RoleEnvironment();
        }        
    }
}
