namespace Microsoft.ApplicationInsights.HostingStartup.Tests
{
    using System.IO;
    using System.Xml.Linq;
    using Microsoft.Web.XmlTransform;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebConfigTransformTest
    {
        private const string InstallConfigTransformationResourceName = "Microsoft.ApplicationInsights.HostingStartup.Tests.Resources.web.config.install.xdt";
      
        [TestMethod]
        public void VerifyInstallationWithBasicWebConfig()
        {
            const string OriginalWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules>
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" /> 
                        </httpModules>
                    </system.web>
                    <system.webServer>
                        <validation validateIntegratedModeConfiguration=""false"" />
                        <modules>
                           <add name=""TelemetryCorrelationHttpModule"" type=""Microsoft.AspNet.TelemetryCorrelation.TelemetryCorrelationHttpModule, Microsoft.AspNet.TelemetryCorrelation"" preCondition=""integratedMode,managedHandler"" />
                           <remove name=""ApplicationInsightsWebTracking"" />
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler""/> 
                        </modules>
                    </system.webServer>
                </configuration>";

            const string ExpectedWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules>
                        </httpModules>
                    </system.web>
                    <system.webServer>
                        <validation validateIntegratedModeConfiguration=""false"" />
                        <modules>
                            <remove name=""ApplicationInsightsWebTracking"" />
                        </modules>
                    </system.webServer>
                </configuration>";

            var transformedWebConfig = this.ApplyUninstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationWithUserModules()
        {
            const string OriginalWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules>
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web""/>
                           <add name=""SomeModule"" type=""Microsoft.ApplicationInsights.Web.RequestTracking.SomeModule, Microsoft.ApplicationInsights.Platform"" /> 
                        </httpModules>
                    </system.web>
                    <system.webServer>
                        <modules runAllManagedModulesForAllRequests=""true"">
                           <remove name=""ApplicationInsightsWebTracking"" />
                           <add name=""UserModule"" type=""UserNamespace.WebModuleFoo""/> 
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler""/> 
                           <add name=""TelemetryCorrelationHttpModule"" type=""Microsoft.AspNet.TelemetryCorrelation.TelemetryCorrelationHttpModule, Microsoft.AspNet.TelemetryCorrelation"" preCondition=""integratedMode,managedHandler"" />
                        </modules>
                    </system.webServer>
                </configuration>";

            const string ExpectedWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules>
                           <add name=""SomeModule"" type=""Microsoft.ApplicationInsights.Web.RequestTracking.SomeModule, Microsoft.ApplicationInsights.Platform""/> 
                        </httpModules>
                    </system.web>
                    <system.webServer>
                        <modules runAllManagedModulesForAllRequests=""true"">
                           <remove name=""ApplicationInsightsWebTracking"" />
                           <add name=""UserModule"" type=""UserNamespace.WebModuleFoo""/> 
                        </modules>
                    </system.webServer>
                </configuration>";

            var transformedWebConfig = this.ApplyUninstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        private XDocument ApplyInstallTransformation(string originalConfiguration, string resourceName)
        {
            return this.ApplyTransformation(originalConfiguration, resourceName);
        }

        private XDocument ApplyUninstallTransformation(string originalConfiguration, string resourceName)
        {
            return this.ApplyTransformation(originalConfiguration, resourceName);
        }

        private void VerifyTransformation(string expectedConfigContent, XDocument transformedWebConfig)
        {
            Assert.IsTrue(
               XNode.DeepEquals(
               transformedWebConfig.FirstNode,
               XDocument.Parse(expectedConfigContent).FirstNode));
        }

        private XDocument ApplyTransformation(string originalConfiguration, string transformationResourceName)
        {
            XDocument result;
            Stream stream = null;
            try
            {
                stream = typeof(WebConfigTransformTest).Assembly.GetManifestResourceStream(transformationResourceName);
                var document = new XmlTransformableDocument();
                using (var transformation = new XmlTransformation(stream, null))
                {
                    stream = null;
                    document.LoadXml(originalConfiguration);
                    transformation.Apply(document);
                    result = XDocument.Parse(document.OuterXml);
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }

            return result;
        }
    }
}
