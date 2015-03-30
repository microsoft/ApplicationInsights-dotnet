namespace Microsoft.ApplicationInsights.AspNet.Tests
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Mvc.Rendering;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.Framework.DependencyInjection.Fallback;
    using System;
    using Xunit;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.ModelBinding;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.IO;

    public class ApplicationInsightsExtensionsTests
    {
        [Fact]
        public void AddTelemetryWillNotThrowWithoutInstrumentationKey()
        {
            try
            {
                var serviceCollection = HostingServices.Create(null);

                //Empty configuration that doesn't have instrumentation key
                IConfiguration config = new Configuration();

                serviceCollection.AddApplicationInsightsTelemetry(config);
            }
            finally
            {
                CleanActiveConfiguration();
            }
        }

        [Fact]
        public void AddTelemetryWillNotUseInstrumentationKeyFromConfig()
        {
            try
            {
                var serviceCollection = HostingServices.Create(null);
                IConfiguration config = new Configuration()
                    .AddJsonFile("content\\config.json");

                serviceCollection.AddApplicationInsightsTelemetry(config);

                Assert.Equal("11111111-2222-3333-4444-555555555555", TelemetryConfiguration.Active.InstrumentationKey);
            }
            finally
            {
                CleanActiveConfiguration();
            }
        }

        [Fact]
        public void AddTelemetryWillCreateTelemetryClientSingleton()
        {
            try
            {
                var serviceCollection = HostingServices.Create(null);
                IConfiguration config = new Configuration()
                    .AddJsonFile("content\\config.json");

                serviceCollection.AddApplicationInsightsTelemetry(config);

                Assert.Equal("11111111-2222-3333-4444-555555555555", TelemetryConfiguration.Active.InstrumentationKey);
                var serviceProvider = serviceCollection.BuildServiceProvider();

                var telemetryClient = serviceProvider.GetService<TelemetryClient>();
                Assert.NotNull(telemetryClient);
                Assert.Equal("11111111-2222-3333-4444-555555555555", telemetryClient.Context.InstrumentationKey);

            }
            finally
            {
                CleanActiveConfiguration();
            }
        }

        [Fact]
        public void JSSnippetWillNotThrowWithoutInstrumentationKey()
        {
            HtmlHelperMock helper = new HtmlHelperMock();
            helper.ApplicationInsightsJavaScriptSnippet(null);
            helper.ApplicationInsightsJavaScriptSnippet("");
        }

        [Fact]
        public void JSSnippetUsesInstrumentationKey()
        {
            var key = "1236543";
            HtmlHelperMock helper = new HtmlHelperMock();
            var result = helper.ApplicationInsightsJavaScriptSnippet(key);
            using (StringWriter sw = new StringWriter())
            {
                result.WriteTo(sw);
                Assert.Contains(key, sw.ToString());
            }
        }


        private class HtmlHelperMock : IHtmlHelper
        {
            public Html5DateRenderingMode Html5DateRenderingMode
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public string IdAttributeDotReplacement
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public IModelMetadataProvider MetadataProvider
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public dynamic ViewBag
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ViewContext ViewContext
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ViewDataDictionary ViewData
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public HtmlString ActionLink(string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public HtmlString AntiForgeryToken()
            {
                throw new NotImplementedException();
            }

            public MvcForm BeginForm(string actionName, string controllerName, object routeValues, FormMethod method, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public MvcForm BeginRouteForm(string routeName, object routeValues, FormMethod method, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public HtmlString CheckBox(string name, bool? isChecked, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public HtmlString Display(string expression, string templateName, string htmlFieldName, object additionalViewData)
            {
                throw new NotImplementedException();
            }

            public string DisplayName(string expression)
            {
                throw new NotImplementedException();
            }

            public string DisplayText(string name)
            {
                throw new NotImplementedException();
            }

            public HtmlString DropDownList(string name, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public HtmlString Editor(string expression, string templateName, string htmlFieldName, object additionalViewData)
            {
                throw new NotImplementedException();
            }

            public string Encode(string value)
            {
                throw new NotImplementedException();
            }

            public string Encode(object value)
            {
                throw new NotImplementedException();
            }

            public void EndForm()
            {
                throw new NotImplementedException();
            }

            public string FormatValue(object value, string format)
            {
                throw new NotImplementedException();
            }

            public string GenerateIdFromName(string name)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, string name)
            {
                throw new NotImplementedException();
            }

            public HtmlString Hidden(string name, object value, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public string Id(string name)
            {
                throw new NotImplementedException();
            }

            public HtmlString Label(string expression, string labelText, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public HtmlString ListBox(string name, IEnumerable<SelectListItem> selectList, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public string Name(string name)
            {
                throw new NotImplementedException();
            }

            public Task<HtmlString> PartialAsync(string partialViewName, object model, ViewDataDictionary viewData)
            {
                throw new NotImplementedException();
            }

            public HtmlString Password(string name, object value, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public HtmlString RadioButton(string name, object value, bool? isChecked, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public HtmlString Raw(object value)
            {
                throw new NotImplementedException();
            }

            public HtmlString Raw(string value)
            {
                throw new NotImplementedException();
            }

            public Task RenderPartialAsync(string partialViewName, object model, ViewDataDictionary viewData)
            {
                throw new NotImplementedException();
            }

            public HtmlString RouteLink(string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public HtmlString TextArea(string name, string value, int rows, int columns, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public HtmlString TextBox(string name, object value, string format, object htmlAttributes)
            {
                throw new NotImplementedException();
            }

            public HtmlString ValidationMessage(string modelName, string message, object htmlAttributes, string tag)
            {
                throw new NotImplementedException();
            }

            public HtmlString ValidationSummary(bool excludePropertyErrors, string message, object htmlAttributes, string tag)
            {
                throw new NotImplementedException();
            }

            public string Value(string name, string format)
            {
                throw new NotImplementedException();
            }
        }

        private void CleanActiveConfiguration()
        {
            TelemetryConfiguration.Active.InstrumentationKey = "";
            TelemetryConfiguration.Active.ContextInitializers.Clear();
            TelemetryConfiguration.Active.TelemetryModules.Clear();
            TelemetryConfiguration.Active.TelemetryInitializers.Clear();
        }


    }
}