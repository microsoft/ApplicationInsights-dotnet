namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
    using System.IO;
    using System.Threading;

    internal class AzureRoleEnvironmentContextReader : IAzureRoleEnvironmentContextReader
    {
        /// <summary>
        /// The singleton instance for our reader.
        /// </summary>
        private static IAzureRoleEnvironmentContextReader instance;

        /// <summary>
        /// The Azure role name (if any).
        /// </summary>
        private string roleName = string.Empty;

        /// <summary>
        /// The Azure role instance name (if any).
        /// </summary>
        private string roleInstanceName = string.Empty;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureRoleEnvironmentContextReader"/> class.
        /// </summary>
        internal AzureRoleEnvironmentContextReader()
        {
        }

        /// <summary>
        /// Gets or sets the singleton instance for our application context reader.
        /// </summary>
        public static IAzureRoleEnvironmentContextReader Instance
        {
            get
            {
                if (AzureRoleEnvironmentContextReader.instance != null)
                {
                    return AzureRoleEnvironmentContextReader.instance;
                }

                if (string.IsNullOrEmpty(AzureRoleEnvironmentContextReader.BaseDirectory) == true)
                {
                    AzureRoleEnvironmentContextReader.BaseDirectory = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "bin");
                }

                Interlocked.CompareExchange(ref AzureRoleEnvironmentContextReader.instance, new AzureRoleEnvironmentContextReader(), null);
                AzureRoleEnvironmentContextReader.instance.Initialize();
                return AzureRoleEnvironmentContextReader.instance;
            }

            // allow for the replacement for the context reader to allow for testability
            internal set
            {
                AzureRoleEnvironmentContextReader.instance = value;
            }
        }

        /// <summary>
        /// Gets or sets the base directly where hunting for application DLLs is to start.
        /// </summary>
        internal static string BaseDirectory { get; set; }

        /// <summary>
        /// Initializes the current reader with respect to its environment.
        /// </summary>
        public void Initialize()
        {
            ServiceRuntime serviceRuntime = new ServiceRuntime();
            RoleEnvironment roleEnvironment = serviceRuntime.GetRoleEnvironment(AzureRoleEnvironmentContextReader.BaseDirectory);

            if (roleEnvironment.IsAvailable == true)
            {
                RoleInstance roleInstance = roleEnvironment.CurrentRoleInstance;
                if (roleInstance != null)
                {
                    this.roleInstanceName = roleEnvironment.CurrentRoleInstance.Id;
                    Role role = roleInstance.Role;
                    if (role != null)
                    {
                        this.roleName = role.Name;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Azure role name.
        /// </summary>
        /// <returns>The extracted data.</returns>
        public string GetRoleName()
        {
            return this.roleName;
        }

        /// <summary>
        /// Gets the Azure role instance name.
        /// </summary>
        /// <returns>The extracted data.</returns>
        public string GetRoleInstanceName()
        {
            return this.roleInstanceName;
        }
    }
}
