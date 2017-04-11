namespace WindowsServer.Nuget.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.Web.XmlTransform;

    public static class ConfigurationHelpers
    {
        private const string ApplicationInsightsConfigInstall = "WindowsServer.Nuget.Tests.ApplicationInsights.config.install.xdt";
        private const string ApplicationInsightsConfigUninstall = "WindowsServer.Nuget.Tests.ApplicationInsights.config.uninstall.xdt";
        private const string ApplicationInsightsTransform = "WindowsServer.Nuget.Tests.ApplicationInsights.config.transform";
        
        private static readonly XNamespace XmlNamespace = "http://schemas.microsoft.com/ApplicationInsights/2013/Settings";

        public static string GetEmptyConfig()
        {
            Stream stream = typeof(TelemetryInitailizersTests).Assembly.GetManifestResourceStream(ApplicationInsightsTransform);
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

        public static string GetPartialTypeName(Type typeToFind)
        {
            return typeToFind.FullName + ", " + typeToFind.Assembly.GetName().Name;
        }

        public static XDocument InstallTransform(string sourceXml)
        {
            return Transform(sourceXml, ApplicationInsightsConfigInstall);
        }

        public static XDocument UninstallTransform(string sourceXml)
        {
            return Transform(sourceXml, ApplicationInsightsConfigUninstall);
        }

        private static XDocument Transform(string sourceXml, string transformationFileResourceName)
        {
            using (var document = new XmlTransformableDocument())
            {
                Stream stream = typeof(TelemetryInitailizersTests).Assembly.GetManifestResourceStream(transformationFileResourceName);
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
