namespace PerfTestAppDeployer
{
    using System;
    using System.Linq;

    using FuncTest.Helpers;
    using FuncTest.IIS;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() != 2)
            {
             Console.WriteLine("******************************************************************************************************");
             Console.WriteLine("Usage");
             Console.WriteLine("PerfTestAppDeployer.exe PhysicalPath AppName");
             Console.WriteLine("Sample Usage");
             Console.WriteLine(@"PerfTestAppDeployer.exe C:\Source\Repos\AppDataCollection\Bin\Debug\APMC\Tests\FunctionalTests\TestApps\Aspx451\App MyAppName");
             Console.WriteLine("Please press enter key.....");
             Console.WriteLine(
                    "******************************************************************************************************");
             Console.Read();             
            }
            else
            {
                string appLocation = args[0];
                string webSiteName = args[1];
                string poolName = "PerfAppPool";
                int port = 9090;

                Console.WriteLine("" + appLocation + " " + webSiteName);

                try
                {
                    ACLTools.GetEveryoneAccessToPath(@appLocation);
                    IisApplicationPool applicationPool = new IisApplicationPool(poolName);
                    IisWebSite iisWebSite = new IisWebSite(webSiteName, @appLocation, port, applicationPool);
                    /*
                    IisWebApplication iisWebApplication = new IisWebApplication(
                        "/" + webAppName,
                        @appLocation,
                        iisWebSite,
                        applicationPool);
                    */

                    Console.WriteLine("Application Deployed to IIS Successfully");
                    Console.WriteLine("ApplicationName:" + webSiteName);
                    Console.WriteLine("PhysicalPath:" + appLocation);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Failure" + exception);
                }
            }            
        }
    }
}
