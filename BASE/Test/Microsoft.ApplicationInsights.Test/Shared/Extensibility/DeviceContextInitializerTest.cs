#if !WINRT
namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Globalization;
#if NET35 || NET40 || NET45
    using System.Management;
#endif
    using System.Reflection;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
#if WINDOWS_PHONE || WINDOWS_PHONE_APP || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if WINRT
    using Windows.ApplicationModel.Core;
    using Windows.Graphics.Display;
    using Windows.Security.ExchangeActiveSyncProvisioning;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
#endif
    using Assert = Xunit.Assert;

    /// <summary>
    /// Device telemetry source tests.
    /// </summary>
    [TestClass]
    public partial class DeviceContextInitializerTest
    {
        [TestMethod]
        public void DeviceContextInitializerClassIsPublicToEnableInstantiation()
        {
#if NET40 || NET45
            Assert.True(typeof(DeviceContextInitializer).IsPublic);
#else
            Assert.True(typeof(DeviceContextInitializer).GetTypeInfo().IsPublic);
#endif
        }

        [TestMethod]
        public void CallingInitializeOnDeviceContextInitializerWithNullThrowsArgumentNullException()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            Assert.Throws<ArgumentNullException>(() => source.Initialize(null));
        }

        [TestMethod]
        public void ReadingDeviceTypeYieldsCorrectValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.Type);

            source.Initialize(telemetryContext);

            string expected = "Other";
#if SILVERLIGHT || WINDOWS_PHONE
            expected = "Phone";
#elif NET35 || NET40 || NET45
            expected = "PC";
#endif
            Assert.Equal(expected, telemetryContext.Device.Type);
        }

        [TestMethod]
        public void ReadingDeviceUniqueIdYieldsStableValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.Id);

            source.Initialize(telemetryContext);

            string id = telemetryContext.Device.Id;
            Assert.Equal(false, id.IsNullOrWhiteSpace());

            // clear the fallback context and expect the same result
            DeviceContextReader.Instance = new DeviceContextReader();
            telemetryContext.Device.Id = string.Empty;

            // second run
            source.Initialize(telemetryContext);

            Assert.Equal(id, telemetryContext.Device.Id);
        }
        
        [TestMethod]
        public void ReadingOSYieldsStableValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.OperatingSystem);

            source.Initialize(telemetryContext);

            string version = null;
            int retryCount = 100;
            while (retryCount > 0)
            {
                version = telemetryContext.Device.OperatingSystem;
                if (string.IsNullOrEmpty(version) == false)
                {
                    break;
                }

                retryCount--;
                Thread.Sleep(100);
            }

            Assert.Equal(false, version.IsNullOrWhiteSpace());

            // clear the fallback context and expect the same result
            DeviceContextReader.Instance = new DeviceContextReader();
            telemetryContext.Device.OperatingSystem = string.Empty;

            // second run
            source.Initialize(telemetryContext);

            retryCount = 100;
            while (retryCount > 0)
            {
                if (string.IsNullOrEmpty(telemetryContext.Device.OperatingSystem) == false)
                {
                    break;
                }

                retryCount--;
                Thread.Sleep(100);
            }

            Assert.Equal(version, telemetryContext.Device.OperatingSystem);
        }

        [TestMethod]
        public void ReadingOSYieldsCorrectValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.OperatingSystem);

            source.Initialize(telemetryContext);

            string version = null;
            int retryCount = 100;
            while (retryCount > 0)
            {
                version = telemetryContext.Device.OperatingSystem;
                if (string.IsNullOrEmpty(version) == false)
                {
                    break;
                }

                retryCount--;
                Thread.Sleep(100);
            }
            
#if WINDOWS_STORE
            Assert.Equal("Windows NT 6.3.9600.16384", version);            
#elif !SILVERLIGHT && !NET35 && !NET40 && !NET45
            Assert.Equal("Windows NT 6.3.9651.0", version);            
#else
            Assert.Equal(string.Format(CultureInfo.InvariantCulture, "Windows NT {0}", Environment.OSVersion.Version.ToString(4)), version);
#endif
        }

        [TestMethod]
        public void ReadingOemNameYieldsStableValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.OemName);

            source.Initialize(telemetryContext);

            string manufacturer = telemetryContext.Device.OemName;
            Assert.Equal(false, manufacturer.IsNullOrWhiteSpace());

            // clear the fallback context and expect the same result
            DeviceContextReader.Instance = new DeviceContextReader();
            telemetryContext.Device.OemName = string.Empty;

            // second run
            source.Initialize(telemetryContext);

            Assert.Equal(manufacturer, telemetryContext.Device.OemName);
        }
        
        [TestMethod]
        public void ReadingDeviceManufacturerYieldsCorrectValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.OemName);

            source.Initialize(telemetryContext);

            string manufacturer = telemetryContext.Device.OemName;
            string expected = "Microsoft";
#if NET35 || NET40 || NET45
            this.RunWmiQuery("Win32_ComputerSystem", "Manufacturer", ref expected);
#elif WINDOWS_STORE
            EasClientDeviceInformation deviceInfo = new EasClientDeviceInformation();
            expected = deviceInfo.SystemManufacturer;
#endif
            Assert.Equal(expected, telemetryContext.Device.OemName);
        }

        [TestMethod]
        public void ReadingDeviceModelYieldsStableValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.Model);

            source.Initialize(telemetryContext);

            string model = telemetryContext.Device.Model;
            Assert.Equal(false, model.IsNullOrWhiteSpace());

            // clear the fallback context and expect the same result
            DeviceContextReader.Instance = new DeviceContextReader();
            telemetryContext.Device.Model = string.Empty;

            // second run
            source.Initialize(telemetryContext);

            Assert.Equal(model, telemetryContext.Device.Model);
        }

        [TestMethod]
        public void ReadingDeviceModelYieldsCorrectValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.Model);

            source.Initialize(telemetryContext);

            string manufacturer = telemetryContext.Device.Model;
            string expected = null;
#if SILVERLIGHT
            expected = "XDeviceEmulator";
#elif NET35 || NET40 || NET45
            this.RunWmiQuery("Win32_ComputerSystem", "Model", ref expected);
#elif WINDOWS_STORE
            EasClientDeviceInformation deviceInfo = new EasClientDeviceInformation();
            expected = deviceInfo.SystemProductName;
#else
            expected = "Virtual";
#endif
            Assert.Equal(expected, manufacturer);
        }

        [TestMethod]
        public void ReadingNetworkTypeYieldsStableValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            source.Initialize(telemetryContext);

            string networkType = telemetryContext.Device.NetworkType;
            Assert.NotEqual("0", networkType);

            // clear the fallback context and expect the same result
            DeviceContextReader.Instance = new DeviceContextReader();
            telemetryContext.Device.NetworkType = "0";

            // second run
            source.Initialize(telemetryContext);

            Assert.Equal(networkType, telemetryContext.Device.NetworkType);
        }

        [TestMethod]
        public void ReadingNetworkTypeYieldsCorrectValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            source.Initialize(telemetryContext);

            string networkType = telemetryContext.Device.NetworkType;
            Assert.Equal("6", networkType);
        }
        
        [TestMethod]
        public void ReadingScreenResolutionYieldsStableValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.ScreenResolution);

            const string DefaultValue = "abc";
            telemetryContext.Device.ScreenResolution = DefaultValue;
            source.Initialize(telemetryContext);

            string screenResolution = null;
            int retryCount = 100;
            while (retryCount > 0)
            {
                screenResolution = telemetryContext.Device.ScreenResolution;
                if (string.Compare(screenResolution, DefaultValue, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    break;
                }

                retryCount--;
                Thread.Sleep(100);
            }

#if NET35 || NET40 || NET45
            Assert.True(screenResolution.IsNullOrWhiteSpace());
#else
            Assert.False(screenResolution.IsNullOrWhiteSpace());
#endif

            // clear the fallback context and expect the same result
            DeviceContextReader.Instance = new DeviceContextReader();
            telemetryContext.Device.ScreenResolution = DefaultValue;

            // second run
            source.Initialize(telemetryContext);

            retryCount = 100;
            while (retryCount > 0)
            {
                screenResolution = telemetryContext.Device.ScreenResolution;
                if (string.Compare(screenResolution, DefaultValue, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    break;
                }

                retryCount--;
                Thread.Sleep(100);
            }

            Assert.Equal(screenResolution, telemetryContext.Device.ScreenResolution);
        }

        [TestMethod]
        public void ReadingScreenResolutionYieldsCorrectValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.ScreenResolution);

            source.Initialize(telemetryContext);

            string screenResolution = null;
            int retryCount = 100;
            while (retryCount > 0)
            {
                screenResolution = telemetryContext.Device.ScreenResolution;
                if (string.IsNullOrEmpty(screenResolution) == false)
                {
                    break;
                }

                retryCount--;
                Thread.Sleep(100);
            }

            string expected = "480x800";
#if NET35 || NET40 || NET45
            expected = null;
#elif WINDOWS_STORE
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            DispatchedHandler executor = () =>
            {
                double actualHeight = Window.Current.Bounds.Height;
                double actualWidth = Window.Current.Bounds.Width;
                double resolutionScale = (double)DisplayInformation.GetForCurrentView().ResolutionScale / 100;
                expected = string.Format(CultureInfo.InvariantCulture, "{0}x{1}", (int)(actualWidth * resolutionScale), (int)(actualHeight * resolutionScale));   
            };

            dispatcher.RunAsync(CoreDispatcherPriority.Low, executor).GetAwaiter().GetResult();
#endif
            Assert.Equal(expected, screenResolution);
        }

        [TestMethod]
        public void ReadingLanguageYieldsStableValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.Language);

            source.Initialize(telemetryContext);

            string locate = telemetryContext.Device.Language;
            Assert.Equal(false, locate.IsNullOrWhiteSpace());

            // clear the fallback context and expect the same result
            DeviceContextReader.Instance = new DeviceContextReader();
            telemetryContext.Device.Language = string.Empty;

            // second run
            source.Initialize(telemetryContext);

            Assert.Equal(locate, telemetryContext.Device.Language);
        }

        [TestMethod]
        public void ReadingLanguageYieldsCorrectValue()
        {
            DeviceContextInitializer source = new DeviceContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Device.Language);

            source.Initialize(telemetryContext);

            string manufacturer = telemetryContext.Device.Language;
            Assert.Equal("en-US", manufacturer);
        }

#if NET35 || NET40 || NET45
        private void RunWmiQuery(string table, string property, ref string output)
        {
            output = "Unknown";
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(string.Format(CultureInfo.InvariantCulture, "SELECT {0} FROM {1}", property, table)))
                {
                    foreach (ManagementObject currentObj in searcher.Get())
                    {
                        object data = currentObj[property];
                        if (data != null)
                        {
                            output = data.ToString();
                            return;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // a lot of things can go wrong with WMI ... as such, apply Pokemon pattern ...
            }
        }
#endif
    }
}
#endif