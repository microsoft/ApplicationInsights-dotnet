namespace TestApp45.Aspx
{
    using System;

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