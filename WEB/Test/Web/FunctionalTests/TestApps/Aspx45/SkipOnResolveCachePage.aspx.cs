namespace Wa45Aspx
{
    using System;

    public partial class SkipOnResolveCachePage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            throw new ApplicationException("OnResolveCache will be sskipped because of this exception.");
        }
    }
}