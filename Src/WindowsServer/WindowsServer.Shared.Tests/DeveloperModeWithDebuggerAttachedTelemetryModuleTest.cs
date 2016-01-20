namespace Microsoft.ApplicationInsights.WindowsServer
{
    using Microsoft.ApplicationInsights.Extensibility;
    
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public class DeveloperModeWithDebuggerAttachedTelemetryModuleTest
    {
        [TestMethod]
        public void DevModeModuleValidateValueNotOverridden()
        {
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
            DeveloperModeWithDebuggerAttachedTelemetryModule module = new DeveloperModeWithDebuggerAttachedTelemetryModule();
            module.Initialize(TelemetryConfiguration.Active);
            Assert.AreEqual(true, TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode);

            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = false;
            module.Initialize(TelemetryConfiguration.Active);
            Assert.AreEqual(false, TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode);
        }

        [TestMethod]
        public void DevModeModuleValidateValueSetToDebuggerAttachedValueWhenNull()
        {
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = null;

            DeveloperModeWithDebuggerAttachedTelemetryModule.IsDebuggerAttached = () => true;
            DeveloperModeWithDebuggerAttachedTelemetryModule module = new DeveloperModeWithDebuggerAttachedTelemetryModule();
            module.Initialize(TelemetryConfiguration.Active);
            Assert.AreEqual(true, TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode);

            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = null;
            DeveloperModeWithDebuggerAttachedTelemetryModule.IsDebuggerAttached = () => false;
            module = new DeveloperModeWithDebuggerAttachedTelemetryModule();
            module.Initialize(TelemetryConfiguration.Active);
            
            Assert.AreEqual(null, TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode);
        }
    }
}
