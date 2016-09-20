namespace FuncTest.Helpers
{
    using System;
    using System.Diagnostics;
    using FuncTest.IIS;
    using Microsoft.Deployment.WindowsInstaller;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Helper class that deploys test applications and provides commaon validation methods
    /// </summary>
    internal static class DeploymentAndValidationTools
    {
        /// <summary>
        /// Folder for ASPX 4.5.1 test application deployment.
        /// </summary>        
        public const string Aspx451AppFolder = ".\\Aspx451";

        /// <summary>
        /// Folder for ASPX 4.5.1 Win32 mode test application deployment.
        /// </summary>        
        public const string Aspx451AppFolderWin32 = ".\\Aspx451Win32";

        /// <summary>
        /// Sleep time to give SDK some time to send events.
        /// </summary>
        public const int SleepTimeForSdkToSendEvents = 10 * 1000;

        /// <summary>
        /// The fake endpoint to which SDK tries to sent Events for the test app ASPX 4.5.1. This should match the one used in
        /// ApplicationInsights.config for the test app being tested.
        /// </summary>
        private const string Aspx451FakeDataPlatformEndpoint = "http://localHost:8789/";

        private const int Aspx451Port = 789;

        private const int Aspx451PortWin32 = 790;

        private static readonly object lockObj = new object();

        private static bool isInitialized;

        public static DependencySourceType ExpectedSource { get; private set; }

        public static HttpListenerObservable SdkEventListener { get; private set; }

        public static TestWebApplication Aspx451TestWebApplication { get; private set; }

        public static TestWebApplication Aspx451TestWebApplicationWin32 { get; private set; }

        /// <summary>
        /// Deploy all test applications and prepera infra.
        /// </summary>
        public static void Initialize()
        {
            if (!isInitialized)
            {
                lock (lockObj)
                {
                    if (!isInitialized)
                    {
                        Aspx451TestWebApplication = new TestWebApplication
                        {
                            AppName = "Aspx451",
                            Port = Aspx451Port,
                            IsRedFieldApp = false
                        };

                        Aspx451TestWebApplicationWin32 = new TestWebApplication
                        {
                            AppName = "Aspx451Win32",
                            Port = Aspx451PortWin32,
                            IsRedFieldApp = false
                        };

                        // this makes all traces have a timestamp so it's easier to troubleshoot timing issues
                        // looking for the better approach...
                        foreach (TraceListener listener in Trace.Listeners)
                        {
                            listener.TraceOutputOptions |= TraceOptions.DateTime;
                        }

                        SdkEventListener = new HttpListenerObservable(Aspx451FakeDataPlatformEndpoint);

                        Aspx451TestWebApplication.Deploy();
                        Aspx451TestWebApplicationWin32.Deploy(true);

                        if (RegistryCheck.IsNet46Installed)
                        {
                            // .NET 4.6 onwards, there is no need of installing agent
                            ExpectedSource = !RegistryCheck.IsStatusMonitorInstalled
                                ? DependencySourceType.Aic
                                : DependencySourceType.Apmc;
                        }
                        else
                        {
                            ExpectedSource = DependencySourceType.Apmc;

                            if (!RegistryCheck.IsStatusMonitorInstalled)
                            {
                                Installer.SetInternalUI(InstallUIOptions.Silent);
                                string installerPath = ExecutionEnvironment.InstallerPath;
                                try
                                {
                                    Installer.InstallProduct(installerPath, "ACTION=INSTALL ALLUSERS=1 MSIINSTALLPERUSER=1");
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError(
                                        "Agent installer not found. Agent is required for running tests for framework version below 4.6" +
                                        ex);
                                    throw;
                                }
                            }
                        }

                        Iis.Reset();

                        isInitialized = true;
                    }
                }
            }
        }

        /// <summary>
        /// Delete all applications and cleanup.
        /// </summary>
        public static void CleanUp()
        {
            if (isInitialized)
            {
                lock (lockObj)
                {
                    if (isInitialized)
                    {
                        SdkEventListener.Dispose();

                        Aspx451TestWebApplication.Remove();
                        Aspx451TestWebApplicationWin32.Remove();

                        if (RegistryCheck.IsNet46Installed)
                        {
                            // .NET 4.6 onwards, there is no need of installing agent 
                        }
                        else
                        {
                            string installerPath = ExecutionEnvironment.InstallerPath;
                            Installer.InstallProduct(installerPath, "REMOVE=ALL");
                            Iis.Reset();
                        }

                        isInitialized = false;
                    }
                }
            }
        }

        /// <summary>
        /// Validates Runtime Dependency Telemetry values.
        /// </summary>        
        /// <param name="itemToValidate">RDD Item to be validated.</param>
        /// <param name="remoteDependencyNameExpected">Expected name.</param>   
        /// <param name="accessTimeMax">Expected maximum limit for access time.</param>   
        /// <param name="successFlagExpected">Expected value for success flag.</param>   
        public static void Validate(
            TelemetryItem<RemoteDependencyData> itemToValidate,
            string remoteDependencyNameExpected,
            TimeSpan accessTimeMax,
            bool successFlagExpected)
        {
            string actualSdkVersion = itemToValidate.tags[new ContextTagKeys().InternalSdkVersion];
            Assert.IsTrue(
                DependencySourceType.Apmc == DeploymentAndValidationTools.ExpectedSource
                    ? actualSdkVersion.Contains("rddp")
                    : actualSdkVersion.Contains("rddf"), "Actual version:" + actualSdkVersion);

            // Validate is within expected limits
            var ticks = (long)(itemToValidate.data.baseData.value * 10000);

            var accessTime = TimeSpan.FromTicks(ticks);

            // DNS resolution may take up to 15 seconds https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.timeout(v=vs.110).aspx.
            // In future when tests will be refactored we should re-think failed http calls validation policy - need to validate resposnes that actually fails on GetResponse, 
            // not only those made to not-existing domain.
            var accessTimeMaxPlusDnsResolutionTime = accessTimeMax.Add(TimeSpan.FromSeconds(15));
            if (successFlagExpected)
            {
                Assert.IsTrue(accessTime.Ticks > 0, "Access time should be above zero");
            }
            else
            {
                Assert.IsTrue(accessTime.Ticks >= 0, "Access time should be zero or above for failed calls");
            }

            Assert.IsTrue(accessTime < accessTimeMaxPlusDnsResolutionTime, string.Format("Access time of {0} exceeds expected max of {1}", accessTime, accessTimeMaxPlusDnsResolutionTime));

            // Validate success flag
            var successFlagActual = itemToValidate.data.baseData.success;
            Assert.AreEqual(successFlagExpected, successFlagActual, "Success flag collected is wrong");
        }
    }
}