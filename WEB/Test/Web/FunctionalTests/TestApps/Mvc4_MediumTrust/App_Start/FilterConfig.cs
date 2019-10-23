using System.Web;
using System.Web.Mvc;

namespace Mvc4_MediumTrust
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}