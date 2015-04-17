namespace Microsoft.ApplicationInsights.AspNet.Tests.Helpers
{
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.ModelBinding;
    using Microsoft.AspNet.Mvc.Rendering;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNet.Mvc.ModelBinding.Validation;
    using Microsoft.Framework.Internal;
    using Microsoft.Framework.WebEncoders;

    internal class HtmlHelperMock : IHtmlHelper
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

        public IHtmlEncoder HtmlEncoder
        {
            get
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

        public IJavaScriptStringEncoder JavaScriptStringEncoder
        {
            get
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

        public ITempDataDictionary TempData
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IUrlEncoder UrlEncoder
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

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelExplorer modelExplorer, string expression)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SelectListItem> GetEnumSelectList(Type enumType)
        {
            throw new NotImplementedException();
        }
        
        public IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct
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
}