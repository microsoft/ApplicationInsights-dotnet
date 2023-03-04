using System;
using System.Text.RegularExpressions;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Environment.SetEnvironmentVariable("Test1", "Value1");
            //Environment.SetEnvironmentVariable("fileSize", "2048");

            //var value = Environment.GetEnvironmentVariable("Test1");
            //var fileSize = String.IsNullOrEmpty(Environment.GetEnvironmentVariable("fileSize")) ? 1024 : Int32.Parse(Environment.GetEnvironmentVariable("fileSize"));
            //var logLevel = String.IsNullOrEmpty(Environment.GetEnvironmentVariable("logLevel")) ? "Error" : Environment.GetEnvironmentVariable("logLevel");

            //Console.WriteLine(fileSize);

            string input = "StudentId= , FirstName=Jack, LastName=Welch";

            input = Regex.Replace(input, @"\s+", "");

            Dictionary<string, string> keyValuePairs = input.Split(',')
              .Select(value => value.Split('='))
              .ToDictionary(pair => pair[0], pair => pair[1]);

            string studentId = keyValuePairs["StudentId"];
            Console.WriteLine(studentId);

            string firstName = keyValuePairs["FirstName"];
            Console.WriteLine(firstName);
            
            string lastName = keyValuePairs["LastName"];
            Console.WriteLine(lastName);

            //C:\LogFile\Location
            //Jack
            //Welch
        }
    }
}