namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Security;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// The .NET 4.0 and 4.5 implementation of the <see cref="IPlatform"/> interface.
    /// </summary>
    internal class PlatformImplementation : IPlatform
    {
        private readonly IDictionary environmentVariables;

        private IDebugOutput debugOutput = null;
        private string hostName;
        private string identityName;

        /// <summary>
        /// Initializes a new instance of the PlatformImplementation class.
        /// </summary>
        public PlatformImplementation()
        {
            try
            {
                this.environmentVariables = Environment.GetEnvironmentVariables();
            }
            catch (Exception e)
            {
                CoreEventSource.Log.FailedToLoadEnvironmentVariables(e.ToString());
            }
        }

        /// <summary>
        /// Returns contents of the ApplicationInsights.config file in the application directory.
        /// </summary>
        public string ReadConfigurationXml()
        {
            string configFilePath;
            try
            {
                // Config file should be in the base directory of the app domain
                configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config");
            }
            catch (SecurityException)
            {
                CoreEventSource.Log.ApplicationInsightsConfigNotAccessibleWarning();
                return string.Empty;
            }

            try
            {
                // Ensure config file actually exists
                if (File.Exists(configFilePath))
                {
                    return File.ReadAllText(configFilePath);
                }
            }
            catch (FileNotFoundException)
            {
                // For cases when file was deleted/modified while reading
                CoreEventSource.Log.ApplicationInsightsConfigNotFoundWarning(configFilePath);
            }
            catch (DirectoryNotFoundException)
            {
                // For cases when file was deleted/modified while reading
                CoreEventSource.Log.ApplicationInsightsConfigNotFoundWarning(configFilePath);
            }
            catch (IOException)
            {
                // For cases when file was deleted/modified while reading
                CoreEventSource.Log.ApplicationInsightsConfigNotFoundWarning(configFilePath);
            }
            catch (UnauthorizedAccessException)
            {
                CoreEventSource.Log.ApplicationInsightsConfigNotFoundWarning(configFilePath);
            }
            catch (SecurityException)
            {
                CoreEventSource.Log.ApplicationInsightsConfigNotFoundWarning(configFilePath);
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the platform specific Debugger writer to the VS output console.
        /// </summary>
        public IDebugOutput GetDebugOutput()
        {
            return this.debugOutput ?? (this.debugOutput = new TelemetryDebugWriter());
        }

        /// <inheritdoc />
        public bool TryGetEnvironmentVariable(string name, out string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            object resultObj = this.environmentVariables?[name];
            value = resultObj?.ToString();
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Returns the machine name.
        /// </summary>
        /// <returns>The machine name.</returns>
        public string GetMachineName()
        {
            return this.hostName ?? (this.hostName = GetHostName());
        }

        public void TestDirectoryPermissions(DirectoryInfo directory)
        {
            string testFileName = Path.GetRandomFileName();
            string testFilePath = Path.Combine(directory.FullName, testFileName);

            if (!Directory.Exists(directory.FullName))
            {
                Directory.CreateDirectory(directory.FullName);
            }

            // Create a test file, will auto-delete this file. 
            using (var testFile = new FileStream(testFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose))
            {
                testFile.Write(new[] { default(byte) }, 0, 1);
            }
        }

        public string GetCurrentIdentityName()
        {
            if (this.identityName == null)
            {
                try
                {
#if NETSTANDARD // This constant is defined for all versions of NetStandard https://docs.microsoft.com/en-us/dotnet/core/tutorials/libraries#how-to-multitarget
                    this.identityName = System.Environment.UserName;
#else
                    using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                    {
                        this.identityName = identity.Name;
                    }
#endif
                }
                catch (SecurityException exp)
                {
                    CoreEventSource.Log.LogWindowsIdentityAccessSecurityException(exp.Message);
                    this.identityName = "Unknown";
                }
            }
            
            return this.identityName;
        }

        private static string GetHostName()
        {
            try
            {
                string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
                string hostName = Dns.GetHostName();

                if (!hostName.EndsWith(domainName, StringComparison.OrdinalIgnoreCase))
                {
                    hostName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", hostName, domainName);
                }

                return hostName;
            }
            catch (Exception ex)
            {
                CoreEventSource.Log.FailedToGetMachineName(ex.Message);
            }

            return string.Empty;
        }
    }
}