namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO.IsolatedStorage;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.Phone.Shell;
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using Assert = Xunit.Assert;

    /// <summary>
    /// Windows Phone (Silverlight runtime) tests for the <see cref="PlatformImplementation"/> class.
    /// </summary>
    public partial class PlatformImplementationTest
    {
        [TestMethod]
        public void GetApplicationSettingsReturnsDictionaryFromIsolatedStorageSettings()
        {
            var platform = new PlatformImplementation();

            IDictionary<string, object> settings = platform.GetApplicationSettings();
            
            Assert.NotNull(settings);
            Assert.Same(IsolatedStorageSettings.ApplicationSettings, settings);
        }
    }
}
