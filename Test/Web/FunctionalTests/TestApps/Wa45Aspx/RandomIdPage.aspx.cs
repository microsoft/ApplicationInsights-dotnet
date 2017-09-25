namespace Wa45Aspx
{
    using System;
    using System.Globalization;
    using System.Web;

    public partial class RandomIdPage : System.Web.UI.Page
    {
        private static int requestCountNotFromCache = 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Cache on server
            DateTime now = DateTime.Now;
            TimeSpan freshness = TimeSpan.FromDays(1);

            Response.Cache.SetExpires(now.Add(freshness));
            Response.Cache.SetMaxAge(freshness);
            Response.Cache.SetCacheability(HttpCacheability.Server);
            Response.Cache.SetValidUntilExpires(true);

            this.Response.AddHeader("Test_Random", Guid.NewGuid().ToString("N"));

            this.label1.InnerText = requestCountNotFromCache.ToString(CultureInfo.InvariantCulture);
            ++requestCountNotFromCache;
        }
    }
}