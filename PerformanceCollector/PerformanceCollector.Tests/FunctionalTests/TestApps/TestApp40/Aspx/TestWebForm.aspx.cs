namespace TestApp40.Aspx
{
    using System;
    using System.IO;

    public partial class TestWebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Clear();
            Response.Write("PerformanceCollector application");
            Response.End();
        }
    }
}