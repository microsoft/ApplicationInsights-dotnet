namespace Wa45Aspx
{
    using System;

    public partial class FlushResponseOnInitPage : System.Web.UI.Page
    {
        protected override void OnInit(EventArgs e)
        {
            // After this Cookie cannot be set and attempt will result in HttpException
            this.Response.Flush();
        } 
    }
}