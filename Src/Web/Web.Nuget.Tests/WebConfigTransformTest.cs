namespace Microsoft.ApplicationInsights.Web
{
    using System.IO;
    using System.Xml.Linq;
    using Microsoft.Web.XmlTransform;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebConfigTransformTest
    {
        private const string InstallConfigTransformationResourceName = "Microsoft.ApplicationInsights.Resources.web.config.install.xdt";
        private const string UninstallConfigTransformationResourceName = "Microsoft.ApplicationInsights.Resources.web.config.uninstall.xdt";

        [TestMethod]
        public void VerifyInstallationToBasicWebConfig()
        {
            const string OriginalWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules />
                    </system.web>
                    <system.webServer>
                        <modules />
                    </system.webServer>
                </configuration>";

            const string ExpectedWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules>
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" /> 
                        </httpModules>
                    </system.web>
                    <system.webServer>
                        <modules>
                           <remove name=""ApplicationInsightsWebTracking"" />
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler""/> 
                        </modules>
                        <validation validateIntegratedModeConfiguration=""false"" />
                    </system.webServer>
                </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyUpdateWithTypeRenamingWebConfig()
        {
            const string OriginalWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules>
                           <add name=""ApplicationInsightsWebTrackingOldName"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" /> 
                        </httpModules>
                    </system.web>
                    <system.webServer>
                        <modules>
                           <add name=""ApplicationInsightsWebTrackingSomeOldName"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler""/> 
                        </modules>
                    </system.webServer>
                </configuration>";

            const string ExpectedWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules>
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" /> 
                        </httpModules>
                    </system.web>
                    <system.webServer>
                        <modules>
                           <remove name=""ApplicationInsightsWebTracking"" />
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler""/> 
                        </modules>
                        <validation validateIntegratedModeConfiguration=""false"" />
                    </system.webServer>
                </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyUninstallationWithBasicWebConfig()
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
                        </modules>
                    </system.webServer>
                </configuration>";

            var transformedWebConfig = this.ApplyUninstallTransformation(OriginalWebConfigContent, UninstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyUninstallationWithUserModules()
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
                           <add name=""UserModule"" type=""UserNamespace.WebModuleFoo""/> 
                        </modules>
                    </system.webServer>
                </configuration>";

            var transformedWebConfig = this.ApplyUninstallTransformation(OriginalWebConfigContent, UninstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToWebConfigWithUserModules()
        {
            const string OriginalWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules>
                           <add name=""SomeModule"" type=""Microsoft.ApplicationInsights.Web.RequestTracking.SomeModule, Microsoft.ApplicationInsights.Platform"" /> 
                        </httpModules>
                    </system.web>
                    <system.webServer>
                        <modules runAllManagedModulesForAllRequests=""true"">
                           <add name=""UserModule"" type=""UserNamespace.WebModuleFoo""/> 
                        </modules>
                    </system.webServer>
                </configuration>";

            const string ExpectedWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules>
                           <add name=""SomeModule"" type=""Microsoft.ApplicationInsights.Web.RequestTracking.SomeModule, Microsoft.ApplicationInsights.Platform""/> 
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web""/>
                        </httpModules>
                    </system.web>
                    <system.webServer>
                        <modules runAllManagedModulesForAllRequests=""true"">
                           <add name=""UserModule"" type=""UserNamespace.WebModuleFoo""/> 
                           <remove name=""ApplicationInsightsWebTracking"" />
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler""/> 
                        </modules>
                        <validation validateIntegratedModeConfiguration=""false"" />
                    </system.webServer>
                </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToEmptyWebConfig()
        {
            const string OriginalWebConfigContent = @"<configuration/>";

            const string ExpectedWebConfigContent = @"
                <configuration>
                    <system.web>
                        <httpModules>
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web""/>
                        </httpModules>
                    </system.web>
                    <system.webServer>
                        <validation validateIntegratedModeConfiguration=""false"" />
                        <modules>
                           <remove name=""ApplicationInsightsWebTracking"" />
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler""/> 
                        </modules>
                    </system.webServer>
                </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToWebConfigWithoutModules()
        {
            const string OriginalWebConfigContent = @"<configuration><system.webServer/></configuration>";

            const string ExpectedWebConfigContent = @"
                <configuration>
                    <system.webServer>
                        <validation validateIntegratedModeConfiguration=""false"" />
                        <modules>
                           <remove name=""ApplicationInsightsWebTracking"" />
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler""/> 
                        </modules>
                    </system.webServer>
                    <system.web>
                        <httpModules>
                           <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web""/>
                        </httpModules>
                    </system.web>
                </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
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
