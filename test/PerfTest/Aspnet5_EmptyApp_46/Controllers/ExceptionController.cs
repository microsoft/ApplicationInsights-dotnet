using Microsoft.AspNet.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Aspnet5_EmptyApp_46.Controllers
{
    public class ExceptionController : Controller
    {
        public string Throw()
        {
            throw new System.Exception("Unhandled Exception from controller.");
        }
    }
}
