namespace FuncTest.Helpers
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using AI;
    using FuncTest.IIS;    
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.VisualStudio.TestTools.UnitTesting;    

    /// <summary>
    /// Helper class that deploys test applications and provides commaon validation methods
    /// </summary>
    internal static class DeploymentAndValidationTools
    {
                
        /// <summary>
        /// Sleep time to give SDK some time to send events.
        /// </summary>
        public const int SleepTimeForSdkToSendEvents = 10 * 1000;

        /// <summary>
        /// Sleep time to give SDK some time to process async events.
        /// </summary>
        public const int SleepTimeForSdkToSendAsyncEvents = 30 * 1000;

        /// <summary>
        /// The fake endpoint to which SDK tries to sent Events for the test app ASPX 4.5.1. This should match the one used in
        /// ApplicationInsights.config for the test app being tested.
        /// </summary>
        private const string Aspx451FakeDataPlatformEndpoint = "http://localHost:8789/";

        public static int Aspx451Port = 789;

        public static int Aspx451PortWin32 = 790;

        public static int AspxCorePort = 791;

        public static int AspxCore20Port = 792;

        private static readonly object lockObj = new object();

        private static bool isInitialized;

        public static string ExpectedSqlSDKPrefix { get; internal set; }
        public static string ExpectedHttpSDKPrefix { get; internal set; }

        public static HttpListenerObservable SdkEventListener { get; private set; }        

        public static EtwEventSessionRdd EtwSession { get; private set; }

        /// <summary>
        /// Prepare common infra for test runs like installing SM, IIS reset if required etc.
        /// </summary>
        public static void Initialize()
        {
            if (!isInitialized)
            {
                lock (lockObj)
                {
                    if (!isInitialized)
                    {
                        // this makes all traces have a timestamp so it's easier to troubleshoot timing issues
                        // looking for the better approach...
                        foreach (TraceListener listener in Trace.Listeners)
                        {
                            listener.TraceOutputOptions |= TraceOptions.DateTime;
                        }

                        SdkEventListener = new HttpListenerObservable(Aspx451FakeDataPlatformEndpoint);

                        EtwSession = new EtwEventSessionRdd();
                        EtwSession.Start();

                        if (RegistryCheck.IsNet46Installed)
                        {
                            Trace.TraceInformation("Detected DotNet46 as installed. Will check StatusMonitor status to determine expected prefix");

                            if(RegistryCheck.IsStatusMonitorInstalled)
                            {
                                Trace.TraceInformation("Detected Status Monitor as installed, ExpectedPrefix: rddp");
                                ExpectedSqlSDKPrefix = "rddp";
                                ExpectedHttpSDKPrefix = "rddp";
                            }
                            else
                            {
                                Trace.TraceInformation("Detected Status Monitor as not installed, ExpectedSqlPrefix: rddf, ExpectedHttpPrefix: rdddsd");
                                ExpectedSqlSDKPrefix = "rddf";
                                ExpectedHttpSDKPrefix = "rdddsd";
                            }                                                        
                        }
                        else
                        {
                            Trace.TraceInformation("Detected DotNet46 as not installed. Will install StatusMonitor if not already installed.");
                            Trace.TraceInformation("Tests against StatusMonitor instrumentation.");
                            ExpectedSqlSDKPrefix = "rddp";
                            ExpectedHttpSDKPrefix = "rddp";

                            if (!RegistryCheck.IsStatusMonitorInstalled)
                            {
                                Trace.TraceInformation("StatusMonitor not already installed.Installing from:" + ExecutionEnvironment.InstallerPath);
                                Installer.SetInternalUI(InstallUIOptions.Silent);
                                string installerPath = ExecutionEnvironment.InstallerPath;
                                try
                                {
                                    Installer.InstallProduct(installerPath, "ACTION=INSTALL ALLUSERS=1 MSIINSTALLPERUSER=1");
                                    Trace.TraceInformation("StatusMonitor installed without errors.");
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError(
                                        "Agent installer not found. Agent is required for running tests for framework version below 4.6" +
                                        ex);
                                    throw;
                                }
                            }
                            else
                            {
                                Trace.TraceInformation("StatusMonitor already installed.");
                            }
                        }                        

                        isInitialized = true;
                    }                    
                }
            }
            else
            {
                Trace.TraceInformation("Already initialized!");
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

                        EtwSession.Stop();

                        if (RegistryCheck.IsNet46Installed)
                        {
                            // .NET 4.6 onwards, there is no need of installing agent 
                        }
                        else
                        {
                            string installerPath = ExecutionEnvironment.InstallerPath;
                            Installer.InstallProduct(installerPath, "REMOVE=ALL");                            
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
        /// <param name="accessTimeMax">Expected maximum limit for access time.</param>   
        /// <param name="successFlagExpected">Expected value for success flag.</param>   
        public static void Validate(
            TelemetryItem<RemoteDependencyData> itemToValidate,
            TimeSpan accessTimeMax,
            bool successFlagExpected,
            string resultCodeExpected = "DontCheck")
        {
            var expectedPrefix = itemToValidate.data.baseData.type == "SQL"
                ? DeploymentAndValidationTools.ExpectedSqlSDKPrefix
                : DeploymentAndValidationTools.ExpectedHttpSDKPrefix;
            string actualSdkVersion = itemToValidate.tags[new ContextTagKeys().InternalSdkVersion];
            Assert.IsTrue(actualSdkVersion.Contains(expectedPrefix), string.Format("Actual version: {0}, Expected prefix: {1}", actualSdkVersion, expectedPrefix));

            if (!resultCodeExpected.Equals("DontCheck"))
            {
                Assert.AreEqual(resultCodeExpected, itemToValidate.data.baseData.resultCode);
            }

            // Validate is within expected limits
            var accessTime = TimeSpan.Parse(itemToValidate.data.baseData.duration, CultureInfo.InvariantCulture);

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

            Assert.IsTrue(accessTime < accessTimeMaxPlusDnsResolutionTime, string.Format(CultureInfo.InvariantCulture, "Access time of {0} exceeds expected max of {1}", accessTime, accessTimeMaxPlusDnsResolutionTime));

            // Validate success flag
            var successFlagActual = itemToValidate.data.baseData.success;
            Assert.AreEqual(successFlagExpected, successFlagActual, "Success flag collected is wrong");
        }
    }
}