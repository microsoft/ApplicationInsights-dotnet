using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.AspNet.Membership.OpenAuth;

namespace Aspx45.Account
{
    public partial class OpenAuthProviders : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            Page.PreRenderComplete += Page_PreRenderComplete;


            if (IsPostBack)
            {
                var provider = Request.Form["provider"];
                if (provider == null)
                {
                    return;
                }

                var redirectUrl = "~/Account/RegisterExternalLogin";
                if (!String.IsNullOrEmpty(ReturnUrl))
                {
                    var resolvedReturnUrl = ResolveUrl(ReturnUrl);
                    redirectUrl += "?ReturnUrl=" + HttpUtility.UrlEncode(resolvedReturnUrl);
                }

                OpenAuth.RequestAuthentication(provider, redirectUrl);
            }
        }


        protected void Page_PreRenderComplete(object sender, EventArgs e)
        {
            providersList.DataSource = OpenAuth.AuthenticationClients.GetAll();
            providersList.DataBind();
        }

        protected T Item<T>() where T : class
        {
            return Page.GetDataItem() as T ?? default(T);
        }


        public string ReturnUrl { get; set; }


    }
}