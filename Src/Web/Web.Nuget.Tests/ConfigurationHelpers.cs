namespace Microsoft.ApplicationInsights.Extensibility.Web
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.Web.XmlTransform;

    public static class ConfigurationHelpers
    {
        private const string ApplicationInsightsConfigInstallNet45 = "Microsoft.ApplicationInsights.Resources.net45.ApplicationInsights.config.install.xdt";
        private const string ApplicationInsightsConfigUninstallNet45 = "Microsoft.ApplicationInsights.Resources.net45.ApplicationInsights.config.uninstall.xdt";
        private const string ApplicationInsightsTransform = "Microsoft.ApplicationInsights.Resources.ApplicationInsights.config.transform";
        
        private static readonly XNamespace XmlNamespace = "http://schemas.microsoft.com/ApplicationInsights/2013/Settings";
        private static string applicationInsightsConfigInstall = ApplicationInsightsConfigInstallNet45;
        private static string applicationInsightsConfigUninstall = ApplicationInsightsConfigUninstallNet45;

        public static void ConfigureNet45()
        {
            applicationInsightsConfigInstall = ApplicationInsightsConfigInstallNet45;
            applicationInsightsConfigUninstall = ApplicationInsightsConfigUninstallNet45;
        }

        public static string GetEmptyConfig()
        {
            Stream stream = typeof(TelemetryInitailizersTestsNet45).Assembly.GetManifestResourceStream(ApplicationInsightsTransform);
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static IEnumerable<XElement> GetTelemetryInitializers(XDocument config)
        {
            return config.Descendants(XmlNamespace + "TelemetryInitializers");
        }

        public static IEnumerable<XElement> GetTelemetryModules(XDocument config)
        {
            return config.Descendants(XmlNamespace + "TelemetryModules");
        }

        public static IEnumerable<XElement> GetTelemetryProcessors(XDocument config)
        {
            return config.Descendants(XmlNamespace + "TelemetryProcessors");
        }

        public static string GetPartialTypeName(Type typeToFind)
        {
            return typeToFind.FullName + ", " + typeToFind.Assembly.GetName().Name;
        }

        public static XDocument InstallTransform(string sourceXml)
        {
            Debug.WriteLine(applicationInsightsConfigInstall);
            return Transform(sourceXml, applicationInsightsConfigInstall);
        }

        public static XDocument UninstallTransform(string sourceXml)
        {
            return Transform(sourceXml, applicationInsightsConfigUninstall);
        }

        private static XDocument Transform(string sourceXml, string transformationFileResourceName)
        {
            using (var document = new XmlTransformableDocument())
            {
                Stream stream = typeof(TelemetryInitailizersTestsNet40).Assembly.GetManifestResourceStream(transformationFileResourceName);
                using (var reader = new StreamReader(stream))
                {
                    string transform = reader.ReadToEnd();
                    using (var transformation = new XmlTransformation(transform, false, null))
                    {
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.ValidationType = ValidationType.None;

                        using (XmlReader xmlReader = XmlReader.Create(new StringReader(sourceXml), settings))
                        {
                            document.Load(xmlReader);
                            transformation.Apply(document);
                            return XDocument.Parse(document.OuterXml);
                        }
                    }
                }
            }
        }
    }
}
