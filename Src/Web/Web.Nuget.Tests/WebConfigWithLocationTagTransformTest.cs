// <copyright file="WebConfigTransformTest.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Web
{
    using System.IO;
    using System.Xml.Linq;
    using Microsoft.Web.XmlTransform;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebConfigWithLocationTagTransformTest
    {
        private const string InstallConfigTransformationResourceName = "Microsoft.ApplicationInsights.Resources.web.config.install.xdt";

        [TestMethod]
        public void VerifyInstallationWhenNonGlobalLocationTagExists()
        {
            const string OriginalWebConfigContent = @"
                    <configuration> 
                      <location path=""a.aspx"">
                        <system.web>
                          <httpModules>
                            <add name=""abc"" type=""type"" />
                          </httpModules>
                        </system.web>
                        <system.webServer>
                          <modules>
                            <add name=""abc"" type=""type"" />
                          </modules>
                          <validation validateIntegratedModeConfiguration=""false"" />
                        </system.webServer>
                      </location>
                    </configuration>";

            const string ExpectedWebConfigContent = @"
                    <configuration>
                      <location path=""a.aspx"">
                        <system.web>
                          <httpModules>
                            <add name=""abc"" type=""type"" />
                          </httpModules>
                        </system.web>
                        <system.webServer>
                          <modules>
                            <add name=""abc"" type=""type"" />
                          </modules>
                          <validation validateIntegratedModeConfiguration=""false"" />
                        </system.webServer>
                      </location>
                      <system.web>
                        <httpModules>
                          <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" />
                        </httpModules>
                      </system.web>
                      <system.webServer>
                        <validation validateIntegratedModeConfiguration=""false"" />
                        <modules>
                          <remove name=""ApplicationInsightsWebTracking"" />
                          <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler"" />
                        </modules>
                      </system.webServer>
                    </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationWhenGlobalAndNonGlobalLocationTagExists()
        {
            const string OriginalWebConfigContent = @"
                    <configuration> 
                        <location path=""a.aspx"">
                        <system.web>
                            <httpModules>
                            <add name=""abc"" type=""type"" />
                            </httpModules>
                        </system.web>
                        <system.webServer>
                            <modules>
                            <add name=""abc"" type=""type"" />
                            </modules>
                            <validation validateIntegratedModeConfiguration=""false"" />
                        </system.webServer>
                        </location>
                        <location path="".""> 
                            <system.web> 
                                <httpModules> 
                                    <add name=""abc"" type=""type"" /> 
                                </httpModules> 
                            </system.web> 
                            <system.webServer>
                                <modules>
                                    <add name=""abc"" type=""type""/>
                                </modules>
                            </system.webServer>
                        </location> 
                    </configuration>";

            const string ExpectedWebConfigContent = @"
                    <configuration>
                        <location path=""a.aspx"">
                        <system.web>
                            <httpModules>
                            <add name=""abc"" type=""type"" />
                            </httpModules>
                        </system.web>
                        <system.webServer>
                            <modules>
                            <add name=""abc"" type=""type"" />
                            </modules>
                            <validation validateIntegratedModeConfiguration=""false"" />
                        </system.webServer>
                        </location>
                        <location path=""."">
                        <system.web>
                            <httpModules>
                            <add name=""abc"" type=""type"" />
                            <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" />
                            </httpModules>
                        </system.web>
                        <system.webServer>
                            <modules>
                            <add name=""abc"" type=""type"" />
                            <remove name=""ApplicationInsightsWebTracking"" />
                            <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler"" />
                            </modules>
                            <validation validateIntegratedModeConfiguration=""false"" />
                        </system.webServer>
                        </location>
                        <system.web>
                        </system.web>
                        <system.webServer>
                        </system.webServer>
                    </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToLocationTagWithDotPathAndExistingModules()
        {
            const string OriginalWebConfigContent = @"
                    <configuration> 
                        <location path="".""> 
                            <system.web> 
                                <httpModules> 
                                    <add name=""abc"" type=""type"" /> 
                                </httpModules> 
                            </system.web> 
                            <system.webServer>
                                <modules>
                                    <add name=""abc"" type=""type""/>
                                </modules>
                            </system.webServer>
                        </location> 
                        <system.web>
                        </system.web>
                        <system.webServer>
                        </system.webServer>
                    </configuration>";

            const string ExpectedWebConfigContent = @"
                    <configuration>
                      <location path=""."">
                        <system.web>
                          <httpModules>
                            <add name=""abc"" type=""type"" />
                            <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" />
                          </httpModules>
                        </system.web>
                        <system.webServer>
                          <modules>
                            <add name=""abc"" type=""type"" />
                            <remove name=""ApplicationInsightsWebTracking"" />
                            <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler"" />
                          </modules>
                          <validation validateIntegratedModeConfiguration=""false"" />
                        </system.webServer>
                      </location>
                      <system.web>
                      </system.web>
                      <system.webServer>
                      </system.webServer>
                    </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToLocationTagWithEmptyPathAndExistingModules()
        {
            const string OriginalWebConfigContent = @"
                    <configuration> 
                        <location> 
                            <system.web> 
                                <httpModules> 
                                    <add name=""abc"" type=""type"" /> 
                                </httpModules> 
                            </system.web> 
                            <system.webServer>
                                <modules>
                                    <add name=""abc"" type=""type""/>
                                </modules>
                            </system.webServer>
                        </location> 
                    </configuration>";

            const string ExpectedWebConfigContent = @"
                    <configuration>
                      <location>
                        <system.web>
                          <httpModules>
                            <add name=""abc"" type=""type"" />
                            <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" />
                          </httpModules>
                        </system.web>
                        <system.webServer>
                          <modules>
                            <add name=""abc"" type=""type"" />
                            <remove name=""ApplicationInsightsWebTracking"" />
                            <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler"" />
                          </modules>
                          <validation validateIntegratedModeConfiguration=""false"" />
                        </system.webServer>
                      </location>
                      <system.web>
                      </system.web>
                      <system.webServer>
                      </system.webServer>
                    </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToLocationTagWithDotPathWithNoModules()
        {
            const string OriginalWebConfigContent = @"
                    <configuration> 
                        <location path="".""> 
                            <system.web> 
                            </system.web> 
                            <system.webServer>
                            </system.webServer>
                        </location> 
                        <system.web> 
                        </system.web> 
                        <system.webServer>
                        </system.webServer>
                    </configuration>";

            const string ExpectedWebConfigContent = @"
                    <configuration>
                      <location path=""."">
                        <system.web>
                          <httpModules>
                            <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" />
                          </httpModules>
                        </system.web>
                        <system.webServer>
                          <validation validateIntegratedModeConfiguration=""false"" />
                          <modules>
                            <remove name=""ApplicationInsightsWebTracking"" />
                            <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler"" />
                          </modules>
                        </system.webServer>
                      </location>
                      <system.web>
                      </system.web>
                      <system.webServer>
                      </system.webServer>
                    </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToLocationTagWithEmptyPathWithNoModules()
        {
            const string OriginalWebConfigContent = @"
                    <configuration> 
                        <location> 
                            <system.web> 
                            </system.web> 
                            <system.webServer>
                            </system.webServer>
                        </location> 
                    </configuration>";

            const string ExpectedWebConfigContent = @"
                    <configuration>
                      <location>
                        <system.web>
                          <httpModules>
                            <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" />
                          </httpModules>
                        </system.web>
                        <system.webServer>
                          <validation validateIntegratedModeConfiguration=""false"" />
                          <modules>
                            <remove name=""ApplicationInsightsWebTracking"" />
                            <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler"" />
                          </modules>
                        </system.webServer>
                      </location>
                      <system.web>
                      </system.web>
                      <system.webServer>
                      </system.webServer>
                    </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToLocationTagWithDotPathWithGlobalModules()
        {
            const string OriginalWebConfigContent = @"
                    <configuration> 
                        <location path="".""> 
                            <system.web> 
                            </system.web> 
                            <system.webServer>
                            </system.webServer>
                        </location> 
                        <system.web> 
                            <httpModules> 
                                <add name=""abc"" type=""type"" /> 
                            </httpModules> 
                        </system.web> 
                        <system.webServer>
                            <modules>
                                <add name=""abc"" type=""type""/>
                            </modules>
                        </system.webServer>
                    </configuration>";

            const string ExpectedWebConfigContent = @"
                    <configuration>
                      <location path=""."">
                        <system.web>
                        </system.web>
                        <system.webServer>
                          <validation validateIntegratedModeConfiguration=""false"" />
                        </system.webServer>
                      </location>
                      <system.web>
                        <httpModules>
                          <add name=""abc"" type=""type"" />
                          <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" />
                        </httpModules>
                      </system.web>
                      <system.webServer>
                        <modules>
                          <add name=""abc"" type=""type"" />
                          <remove name=""ApplicationInsightsWebTracking"" />
                          <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler"" />
                        </modules>
                      </system.webServer>
                    </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToLocationTagWithEmptyPathWithGlobalModules()
        {
            const string OriginalWebConfigContent = @"
                    <configuration> 
                        <location> 
                        </location> 
                        <system.web> 
                            <httpModules> 
                                <add name=""abc"" type=""type"" /> 
                            </httpModules> 
                        </system.web> 
                        <system.webServer>
                            <modules>
                                <add name=""abc"" type=""type""/>
                            </modules>
                        </system.webServer>
                    </configuration>";

            const string ExpectedWebConfigContent = @"
                    <configuration>
                      <location>
                      </location>
                      <system.web>
                        <httpModules>
                          <add name=""abc"" type=""type"" />
                          <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" />
                        </httpModules>
                      </system.web>
                      <system.webServer>
                        <modules>
                          <add name=""abc"" type=""type"" />
                          <remove name=""ApplicationInsightsWebTracking"" />
                          <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler"" />
                        </modules>
                        <validation validateIntegratedModeConfiguration=""false"" />
                      </system.webServer>
                    </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToLocationTagWithDotPathWithGlobalModulesAndExistingValidationTag()
        {
            const string OriginalWebConfigContent = @"
                    <configuration> 
                        <location path="".""> 
                            <system.web> 
                            </system.web> 
                            <system.webServer>
                              <validation validateIntegratedModeConfiguration=""false"" />
                            </system.webServer>
                        </location> 
                        <system.web> 
                            <httpModules> 
                                <add name=""abc"" type=""type"" /> 
                            </httpModules> 
                        </system.web> 
                        <system.webServer>
                            <modules>
                                <add name=""abc"" type=""type""/>
                            </modules>
                        </system.webServer>
                    </configuration>";

            const string ExpectedWebConfigContent = @"
                    <configuration>
                      <location path=""."">
                        <system.web>
                        </system.web>
                        <system.webServer>
                          <validation validateIntegratedModeConfiguration=""false"" />
                        </system.webServer>
                      </location>
                      <system.web>
                        <httpModules>
                          <add name=""abc"" type=""type"" />
                          <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" />
                        </httpModules>
                      </system.web>
                      <system.webServer>
                        <modules>
                          <add name=""abc"" type=""type"" />
                          <remove name=""ApplicationInsightsWebTracking"" />
                          <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler"" />
                        </modules>
                      </system.webServer>
                    </configuration>";

            var transformedWebConfig = this.ApplyInstallTransformation(OriginalWebConfigContent, InstallConfigTransformationResourceName);
            this.VerifyTransformation(ExpectedWebConfigContent, transformedWebConfig);
        }

        [TestMethod]
        public void VerifyInstallationToLocationTagWithEmptyPathWithGlobalModulesAndExistingValidationTag()
        {
            const string OriginalWebConfigContent = @"
                    <configuration> 
                        <location> 
                            <system.webServer>
                              <validation validateIntegratedModeConfiguration=""false"" />
                            </system.webServer>
                        </location> 
                        <system.web> 
                            <httpModules> 
                                <add name=""abc"" type=""type"" /> 
                            </httpModules> 
                        </system.web> 
                        <system.webServer>
                            <modules>
                                <add name=""abc"" type=""type""/>
                            </modules>
                        </system.webServer>
                    </configuration>";

            const string ExpectedWebConfigContent = @"
                    <configuration>
                      <location>
                            <system.webServer>
                              <validation validateIntegratedModeConfiguration=""false"" />
                            </system.webServer>
                      </location>
                      <system.web>
                        <httpModules>
                          <add name=""abc"" type=""type"" />
                          <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" />
                        </httpModules>
                      </system.web>
                      <system.webServer>
                        <modules>
                          <add name=""abc"" type=""type"" />
                          <remove name=""ApplicationInsightsWebTracking"" />
                          <add name=""ApplicationInsightsWebTracking"" type=""Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule, Microsoft.AI.Web"" preCondition=""managedHandler"" />
                        </modules>
                      </system.webServer>
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
