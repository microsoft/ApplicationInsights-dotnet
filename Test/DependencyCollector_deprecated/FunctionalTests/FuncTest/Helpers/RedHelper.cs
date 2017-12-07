// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RedHelper.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
//   The red helper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace FuncTest.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using NuGet;

    /// <summary>
    /// The red helper.
    /// </summary>
    internal static class RedHelper
    {
        #region Constants and Fields

        /// <summary>The application insights config namespace.</summary>
        private const string ApplicationInsightsConfigNamespace =
            "http://schemas.microsoft.com/ApplicationInsights/2013/Settings";

        /// <summary>The nuget server with all packages.</summary>
        private const string NugetServerWithAllPackages = @"http://unicornnuget.cloudapp.net/nuget";

        /// <summary>The status monitor install path.</summary>
        private const string StatusMonitorInstallPath =
            @"C:\Program Files\Microsoft Application Insights\Status Monitor";

        /// <summary>The status monitor nuget id.</summary>
        private const string StatusMonitorNugetId = "Microsoft.ApplicationInsights.StatusMonitor";

        /// <summary>The package version.</summary>
        private static string packageVersion;

        /// <summary> The downloaded packages (temp location). </summary>
        private static string tempPackagesFolder;

        #endregion

        #region Methods

        /// <summary>The cleanup.</summary>
        internal static void Cleanup()
        {
            if (!string.IsNullOrWhiteSpace(tempPackagesFolder))
            {
                DeleteFileSystemInfo(new DirectoryInfo(tempPackagesFolder));
            }

            DeleteOldPackages();
        }

        /// <summary>The initialize.</summary>
        internal static void Initialize()
        {
            // Already initialized
            if (packageVersion != null)
            {
                return;
            }

            DeleteOldPackages();

            tempPackagesFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string installedPackages = Path.Combine(tempPackagesFolder, "Installed");
            string packagesFolder = Path.Combine(tempPackagesFolder, "Packages");
            Directory.CreateDirectory(installedPackages);
            Directory.CreateDirectory(packagesFolder);

            // Analyze StatusMonitor.nuget and find the dependency.
            string nugetFolder = Path.GetDirectoryName(ExecutionEnvironment.StatusMonitorNugetPath);
            IPackageRepository statusMonitorNugetRepo = PackageRepositoryFactory.Default.CreateRepository(nugetFolder);
            IPackage package;
            try
            {
                package = statusMonitorNugetRepo.FindPackage(StatusMonitorNugetId, (IVersionSpec)null, true, true);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format(
                        "Could not find package '{0}' in '{1}': {2}", 
                        StatusMonitorNugetId, 
                        nugetFolder, 
                        ex.Message), 
                    ex);
            }

            PackageDependency dependency;

            try
            {
                PackageDependencySet dependencySet = package.DependencySets.Single();
                dependency = dependencySet.Dependencies.Single();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Dependency.Single threw an exception: '{0}'", ex.Message), ex);
            }

            // Copy to .\Packages folder
            string statusMonitorNugetPath = ExecutionEnvironment.StatusMonitorNugetPath;
            string nugetFileName = Path.GetFileName(statusMonitorNugetPath);
            Debug.Assert(nugetFileName != null, "nugetFileName is null");
            string outputPath = Path.Combine(packagesFolder, nugetFileName);
            File.Copy(statusMonitorNugetPath, outputPath, true);

            // Install all nugets which our StatusMonitor.nuget depends on. Download them from our own private nuget server
            IPackageRepository packagesRepo =
                PackageRepositoryFactory.Default.CreateRepository(NugetServerWithAllPackages);
            var manager = new PackageManager(
                packagesRepo, 
                new DefaultPackagePathResolver(installedPackages), 
                new PhysicalFileSystem(installedPackages));
            try
            {
                manager.InstallPackage(dependency.Id, dependency.VersionSpec.MinVersion, false, true);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format(
                        "Could not install package: '{0}', '{1}' from '{2}': {3}", 
                        dependency.Id, 
                        dependency.VersionSpec.MinVersion, 
                        NugetServerWithAllPackages, 
                        ex.Message), 
                    ex);
            }

            // Copy all *.nupkg files to .\Packages folder
            foreach (string newPath in Directory.GetFiles(installedPackages, "*.nupkg", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(newPath);
                Debug.Assert(fileName != null, "fileName is null");
                string output = Path.Combine(packagesFolder, fileName);
                File.Copy(newPath, output, true);
            }

            // Now let StatusMonitor to download these packages
            string arguments = string.Format(
                "/c {0} download \"{1}\" \"{2}\"", 
                ExecutionEnvironment.StatusMonitorLauncherPath, 
                StatusMonitorInstallPath, 
                packagesFolder);
            ProcessHelper.ExecuteProcess("cmd.exe", arguments, TimeSpan.FromMinutes(1));

            // Get downloaded version
            packageVersion = GetPackageVersion();
        }

        /// <summary>The instrument.</summary>
        /// <param name="appName">The app name.</param>
        /// <param name="appPath">The app path.</param>
        /// <param name="platformEndpoint">The platform endpoint.</param>
        internal static void Instrument(string appName, string appPath, string platformEndpoint)
        {
            // Instrument the app
            string arguments = string.Format(
                "/c {0} instrument \"{1}\" {2} {3}", 
                ExecutionEnvironment.StatusMonitorLauncherPath, 
                StatusMonitorInstallPath, 
                packageVersion, 
                appName);
            ProcessHelper.ExecuteProcess("cmd.exe", arguments, TimeSpan.FromMinutes(1));

            // Now we need to add a setting to override where app will be sending telemetry to.
            string pathToApplicationInsightsConfig = Path.Combine(appPath, "ApplicationInsights.config");

            XNamespace ns = ApplicationInsightsConfigNamespace;
            XDocument doc = XDocument.Load(pathToApplicationInsightsConfig);
            Debug.Assert(doc.Root != null, "doc.Root is null");
            
            doc.Root.AddFirst(
                new XElement(
                    ns + "TelemetryChannel",
                    new XElement(
                        ns + "InProcess",
                        new XElement(ns + "EndpointAddress", platformEndpoint + "v2/track"),
                        new XElement(ns + "DataUploadIntervalInSeconds", "1")),
                    new XElement(ns + "OutOfProcess"),
                    new XElement(ns + "DeveloperMode", "false")));

            doc.Save(pathToApplicationInsightsConfig);
        }

        /// <summary>The instrument.</summary>
        /// <param name="appName">The app name.</param>
        /// <param name="appPath">The app path.</param>
        internal static void Deinstrument(string appName, string appPath)
        {
            // Instrument the app
            string arguments = string.Format(
                "/c {0} instrument \"{1}\" {2} {3} -disable",
                ExecutionEnvironment.StatusMonitorLauncherPath,
                StatusMonitorInstallPath,
                packageVersion,
                appName);
            ProcessHelper.ExecuteProcess("cmd.exe", arguments, TimeSpan.FromMinutes(1));
        }

        /// <summary>The delete file system info.</summary>
        /// <param name="fsi">The fsi.</param>
        private static void DeleteFileSystemInfo(FileSystemInfo fsi)
        {
            fsi.Attributes = FileAttributes.Normal;
            var di = fsi as DirectoryInfo;

            if (di != null)
            {
                foreach (FileSystemInfo dirInfo in di.GetFileSystemInfos())
                {
                    DeleteFileSystemInfo(dirInfo);
                }
            }

            fsi.Delete();
        }

        /// <summary>The delete old packages.</summary>
        private static void DeleteOldPackages()
        {
            string binPath = Path.Combine(StatusMonitorInstallPath, "bin");
            var di = new DirectoryInfo(binPath);
            foreach (FileSystemInfo dirInfo in di.GetFileSystemInfos())
            {
                if (!string.Equals(dirInfo.Name, "Default"))
                {
                    DeleteFileSystemInfo(dirInfo);
                }
            }
        }

        /// <summary>The get package version.</summary>
        /// <returns>The <see cref="string"/>.</returns>
        private static string GetPackageVersion()
        {
            string binPath = Path.Combine(StatusMonitorInstallPath, "bin");
            var di = new DirectoryInfo(binPath);
            foreach (FileSystemInfo dirInfo in di.GetFileSystemInfos())
            {
                if (!string.Equals(dirInfo.Name, "Default"))
                {
                    return dirInfo.Name;
                }
            }

            throw new Exception(string.Format("Could not find downloaded package in '{0}'", binPath));
        }

        #endregion
    }
}