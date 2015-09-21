namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System.Collections;
    using System.IO;
    using System.Linq; 
    using System.Security.AccessControl;
    using System.Security.Principal;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ApplicationFolderProviderTest
    { 
        private DirectoryInfo testDirectory;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (this.testDirectory.Exists)
            {
                this.testDirectory.Delete(true);
            }
        }

        [TestMethod]
        public void GetApplicationFolderReturnsValidPlatformFolder()
        {
            IApplicationFolderProvider provider = new ApplicationFolderProvider();
            IPlatformFolder applicationFolder = provider.GetApplicationFolder();
            Assert.IsNotNull(applicationFolder);
        }

        [TestMethod]
        public void GetApplicationFolderReturnsSubfolderFromLocalAppDataFolder()
        {
            DirectoryInfo localAppData = this.CreateTestDirectory(@"AppData\Local");
            var environmentVariables = new Hashtable { { "LOCALAPPDATA", localAppData.FullName } };
            var provider = new ApplicationFolderProvider(environmentVariables);

            IPlatformFolder applicationFolder = provider.GetApplicationFolder();

            Assert.IsNotNull(applicationFolder);
            Assert.AreEqual(1, localAppData.GetDirectories().Length);
        }

        [TestMethod]
        public void GetApplicationFolderReturnsSubfolderFromTempFolderIfLocalAppDataIsNotAvailable()
        {
            DirectoryInfo temp = this.CreateTestDirectory("Temp");
            var environmentVariables = new Hashtable { { "TEMP", temp.FullName } };
            var provider = new ApplicationFolderProvider(environmentVariables);

            IPlatformFolder applicationFolder = provider.GetApplicationFolder();

            Assert.IsNotNull(applicationFolder);
            Assert.AreEqual(1, temp.GetDirectories().Length);
        }

        [TestMethod]
        public void GetApplicationFolderReturnsSubfolderFromTempFolderIfLocalAppDataIsAvailableButNotAccessible()
        {
            DirectoryInfo localAppData = this.CreateTestDirectory(@"AppData\Local", FileSystemRights.CreateDirectories, AccessControlType.Deny);
            DirectoryInfo temp = this.CreateTestDirectory("Temp");
            var environmentVariables = new Hashtable 
            { 
                { "LOCALAPPDATA", localAppData.FullName },
                { "TEMP", temp.FullName },
            };
            var provider = new ApplicationFolderProvider(environmentVariables);

            IPlatformFolder applicationFolder = provider.GetApplicationFolder();

            Assert.IsNotNull(applicationFolder);
            Assert.AreEqual(1, temp.GetDirectories().Length);
        }

        [TestMethod]
        public void GetApplicationFolderReturnsNullWhenNeitherLocalAppDataNorTempFolderIsAccessible()
        {
            var environmentVariables = new Hashtable 
            { 
                { "LOCALAPPDATA", this.CreateTestDirectory(@"AppData\Local", FileSystemRights.CreateDirectories, AccessControlType.Deny).FullName },
                { "TEMP", this.CreateTestDirectory("Temp", FileSystemRights.CreateDirectories, AccessControlType.Deny).FullName },
            };
            var provider = new ApplicationFolderProvider(environmentVariables);

            IPlatformFolder applicationFolder = provider.GetApplicationFolder();

            Assert.IsNull(applicationFolder);
        }

        [TestMethod]
        public void GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButDeniesRightToListDirectoryContents()
        {
            this.GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButNotAccessible(FileSystemRights.ListDirectory);
        }

        [TestMethod]
        public void GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButDeniesRightToCreateFiles()
        {
            this.GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButNotAccessible(FileSystemRights.CreateFiles);
        }

        [TestMethod]
        public void GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButDeniesRightToWrite()
        {
            this.GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButNotAccessible(FileSystemRights.Write);            
        }

        [TestMethod]
        public void GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButDeniesRightToRead()
        {
            this.GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButNotAccessible(FileSystemRights.Read);
        }

        // TODO: Find way to detect denied FileSystemRights.DeleteSubdirectoriesAndFiles
        public void GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButDeniesRightToDeleteSubdirectoriesAndFiles()
        {
            this.GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButNotAccessible(FileSystemRights.DeleteSubdirectoriesAndFiles);
        }

        private void GetApplicationFolderReturnsNullWhenFolderAlreadyExistsButNotAccessible(FileSystemRights rights)
        {
            DirectoryInfo localAppData = this.CreateTestDirectory(@"AppData\Local");
            var environmentVariables = new Hashtable { { "LOCALAPPDATA", localAppData.FullName } };
            var provider = new ApplicationFolderProvider(environmentVariables);

            // Create the application folder and make it inaccessible
            provider.GetApplicationFolder();
            DirectoryInfo microsoft = localAppData.GetDirectories().Single();
            DirectoryInfo applicationInsights = microsoft.GetDirectories().Single();
            DirectoryInfo application = applicationInsights.GetDirectories().Single();
            using (new DirectoryAccessDenier(application, rights))
            { 
                // Try getting the inaccessible folder
                Assert.IsNull(provider.GetApplicationFolder());
            }
        }

        private DirectoryInfo CreateTestDirectory(string path, FileSystemRights rights = FileSystemRights.FullControl, AccessControlType access = AccessControlType.Allow)
        {
            DirectoryInfo directory = this.testDirectory.CreateSubdirectory(path);
            DirectorySecurity security = directory.GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().Name, rights, access));
            directory.SetAccessControl(security);
            return directory;
        }
    }
}
