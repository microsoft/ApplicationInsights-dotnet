// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExecutionEnvironment.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
//   The execution environment.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace FuncTest.Helpers
{
    using System;
    using System.IO;

    /// <summary>The execution environment.</summary>
    internal class ExecutionEnvironment
    {
        #region Constants and Fields

        /// <summary>The path to bin output of a build on dev box.
        /// Source: C:\src\AppDataCollection\src\APMC\Tests\FunctionalTests\TestResults\Deploy_zakima 2014-09-08 15_40_57\Out
        /// Target: C:\src\AppDataCollection\Bin\Debug</summary>
        private const string BinDevPath = @"..\..\..\..\..\..\..\Bin\Debug";

        #endregion

        #region Properties

        /// <summary>Gets the installer path.</summary>
        internal static string InstallerPath
        {
            get
            {
                // This is the rolling test machine case
                const string ApplicationInsightsAgentPath = @"C:\ApplicationInsightsAgent.msi";
                if (File.Exists(ApplicationInsightsAgentPath))
                {
                    return Path.GetFullPath(ApplicationInsightsAgentPath);
                }

                // This is a dev case
                string path = Path.Combine(BinDevPath, @"AgentInstaller\Setup\ApplicationInsightsAgent.msi");
                return Path.GetFullPath(path);
            }
        }

        /// <summary>Gets the status monitor launcher path.</summary>
        internal static string StatusMonitorLauncherPath
        {
            get
            {
                // This is the rolling test machine case
                const string StatusMonitorRollingTestPath = @"C:\StatusMonitorRollingTest\StatusMonitorRollingTest.exe";
                if (File.Exists(StatusMonitorRollingTestPath))
                {
                    return Path.GetFullPath(StatusMonitorRollingTestPath);
                }

                // This is a dev case
                string path = Path.Combine(
                    BinDevPath, 
                    @"StatusMonitor\StatusMonitor\TestTools\StatusMonitorRollingTest\StatusMonitorRollingTest.exe");
                return Path.GetFullPath(path);
            }
        }

        /// <summary>Gets the status monitor nuget path.</summary>
        internal static string StatusMonitorNugetPath
        {
            get
            {
                // This is the rolling test machine case
                const string ApplicationinsightsStatusMonitorPackageMask =
                    "Microsoft.ApplicationInsights.StatusMonitor.*.nupkg";
                string[] files = Directory.GetFiles(
                    @"c:\", 
                    ApplicationinsightsStatusMonitorPackageMask, 
                    SearchOption.TopDirectoryOnly);
                if (files.Length == 1)
                {
                    return Path.GetFullPath(files[0]);
                }

                // This is a dev case
                string path = Path.Combine(BinDevPath, @"StatusMonitor\Nuget\");
                files = Directory.GetFiles(
                    path, 
                    ApplicationinsightsStatusMonitorPackageMask, 
                    SearchOption.TopDirectoryOnly);
                if (files.Length == 1)
                {
                    return Path.GetFullPath(files[0]);
                }

                throw new Exception("Could not find StatusMonitorNugetPath");
            }
        }

        #endregion
    }
}