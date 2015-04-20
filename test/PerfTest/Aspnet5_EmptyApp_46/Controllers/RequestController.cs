using Microsoft.AspNet.Mvc;

namespace Aspnet5_EmptyApp_46
{
    public class RequestController : Controller
    {
        public string Do(int loadTimeInMs)
        {
            var businessLogic = new CycleBusinessLogic(loadTimeInMs);
            businessLogic.Execute();

            return businessLogic.LogicTime.ToString();
        }        
    }
}