namespace IngestionService
{
    using System.Web;

    public class DataStoragePathMapper
    {
        public string GetDataPath()
        {
            //return HttpContext.Current.Server.MapPath("~/App_Data/");
            return "c:\\temp";
        }
    }
}