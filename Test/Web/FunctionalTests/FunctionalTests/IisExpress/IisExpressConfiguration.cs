namespace Functional.IisExpress
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Web.XmlTransform;

    public class IisExpressConfiguration
    {
        private const string TransformationTemplate = "<?xml version=\"1.0\" encoding=\"utf-8\" ?> " +
                                                            "<configuration xmlns:xdt=\"http://schemas.microsoft.com/XML-Document-Transform\">" +
                                                                    "<system.applicationHost>" +
                                                                       "<sites>" +
                                                                         "<site name=\"{0}\" id=\"1\" serverAutoStart=\"true\" xdt:Transform=\"Replace\"  xdt:Locator=\"Match(id)\"> " +
                                                                            "<application path=\"/\" applicationPool=\"{1}\">" +
                                                                                "<virtualDirectory path=\"/\" physicalPath=\"{2}\"/>" +
                                                                             "</application>" +
                                                                             "<bindings>" +
                                                                                "<binding protocol=\"http\" bindingInformation=\":{3}:localhost\"/>" +
                                                                              "</bindings> " +
                                                                            "</site> " +
                                                                           "</sites>" +
                                                                        "</system.applicationHost>" +
                                                                   "</configuration>";

        private const string ApplicationHostResourceName = "Functional.IisExpress.applicationhost.config";
        private const string DefaultSiteName = "Development Web Site";
        private IDictionary<string, string> environmentVariables = new Dictionary<string, string>();

        public IisExpressAppPools ApplicationPool { get; set; }

        public bool UseX64Process { get; set; }

        public string Site { get; set; }

        public string Path { get; set; }

        public int Port { get; set; }

        public IDictionary<string, string> EnvironmentVariables
        {
            get { return this.environmentVariables; }
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }

                this.environmentVariables = value;
            }
        }

        public string GetConfigFile()
        {
            if (string.IsNullOrEmpty(Path))
            {
                throw new ArgumentNullException("Path");
            }

            if (Port <= 0)
            {
                throw new ArgumentOutOfRangeException("Port");
            }

            return
                SetParametersToConfig(string.Format(TransformationTemplate,
                    string.IsNullOrEmpty(Site) ? DefaultSiteName : Site, ApplicationPool, Path, Port));
        }

        private string SetParametersToConfig(string transformationString)
        {
            using (var applicationHostStream = typeof(IisExpressConfiguration).Assembly.GetManifestResourceStream(ApplicationHostResourceName))
            {
                if (applicationHostStream != null)
                {
                    using (var transformation = new XmlTransformation(transformationString, false, null))
                    {
                        var document = new XmlTransformableDocument();
                        document.Load(applicationHostStream);
                        transformation.Apply(document);

                        return document.OuterXml;
                    }
                }
            }

            return string.Empty;
        }

    }
}
