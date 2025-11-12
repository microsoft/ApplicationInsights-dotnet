namespace BasicConsoleApp
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Demonstrates all the ways to create and track ExceptionTelemetry.
    /// </summary>
    internal class ExceptionTelemetryExamples
    {
        public static void Run(TelemetryClient telemetryClient)
        {
            Console.WriteLine("=== ExceptionTelemetry Examples ===\n");

            // 1. Track exception directly with properties
            Method1_DirectExceptionWithProperties(telemetryClient);

            // 2. Track exception using ExceptionTelemetry object (most common)
            Method2_ExceptionTelemetryConstructor(telemetryClient);

            // 3. Track exception with nested inner exceptions
            Method3_NestedInnerExceptions(telemetryClient);

            // 4. Track AggregateException with multiple inner exceptions
            Method4_AggregateException(telemetryClient);

            // 5. Create ExceptionTelemetry with custom ExceptionDetailsInfo (advanced scenario)
            Method5_CustomExceptionDetailsInfo(telemetryClient);

            // 6. Using Context.GlobalProperties (applies to all telemetry)
            Method6_GlobalProperties(telemetryClient);

            // 7. Parameterless constructor with manual property setting
            Method7_ParameterlessConstructor(telemetryClient);

            // 8. Exception with SetParsedStack (for custom stack trace parsing)
            Method8_SetParsedStack(telemetryClient);

            // 9. Exception with null handling (defensive programming)
            Method9_ExceptionReplacement(telemetryClient);

            Console.WriteLine("\nAll ExceptionTelemetry tracking methods demonstrated!");
        }

        private static void Method1_DirectExceptionWithProperties(TelemetryClient telemetryClient)
        {
            Console.WriteLine("1. Direct exception with properties");
            try
            {
                throw new InvalidOperationException("Method 1: Direct exception with properties");
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex, new Dictionary<string, string> 
                { 
                    { "Method", "1" }, 
                    { "Location", "Main" } 
                });
            }
        }

        private static void Method2_ExceptionTelemetryConstructor(TelemetryClient telemetryClient)
        {
            Console.WriteLine("2. Using ExceptionTelemetry constructor");
            try
            {
                throw new InvalidOperationException("Method 2: Using ExceptionTelemetry constructor");
            }
            catch (Exception ex)
            {
                var exceptionTelemetry = new ExceptionTelemetry(ex);
                exceptionTelemetry.SeverityLevel = SeverityLevel.Error;
                exceptionTelemetry.Properties.Add("Method", "2");
                exceptionTelemetry.Properties.Add("UserAction", "Submit");
                telemetryClient.TrackException(exceptionTelemetry);
            }
        }

        private static void Method3_NestedInnerExceptions(TelemetryClient telemetryClient)
        {
            Console.WriteLine("3. Nested inner exceptions (3 levels)");
            try
            {
                try
                {
                    try
                    {
                        throw new ArgumentException("Innermost exception");
                    }
                    catch (Exception innerEx)
                    {
                        throw new InvalidOperationException("Middle exception", innerEx);
                    }
                }
                catch (Exception middleEx)
                {
                    throw new ApplicationException("Method 3: Outer exception with nested inner exceptions", middleEx);
                }
            }
            catch (Exception ex)
            {
                var exceptionTelemetry = new ExceptionTelemetry(ex);
                exceptionTelemetry.Message = "Custom message for nested exception";
                exceptionTelemetry.Properties.Add("Method", "3");
                telemetryClient.TrackException(exceptionTelemetry);
            }
        }

        private static void Method4_AggregateException(TelemetryClient telemetryClient)
        {
            Console.WriteLine("4. AggregateException with multiple inner exceptions");
            try
            {
                var task1 = Task.Run(() => throw new InvalidOperationException("Task 1 failed"));
                var task2 = Task.Run(() => throw new ArgumentNullException("Task 2 failed"));
                var task3 = Task.Run(() => throw new FormatException("Task 3 failed"));
                Task.WaitAll(task1, task2, task3);
            }
            catch (AggregateException aggEx)
            {
                var exceptionTelemetry = new ExceptionTelemetry(aggEx);
                exceptionTelemetry.Properties.Add("Method", "4");
                exceptionTelemetry.Properties.Add("TaskCount", aggEx.InnerExceptions.Count.ToString());
                telemetryClient.TrackException(exceptionTelemetry);
            }
        }

        private static void Method5_CustomExceptionDetailsInfo(TelemetryClient telemetryClient)
        {
            Console.WriteLine("5. Custom ExceptionDetailsInfo (advanced)");
            
            var customExceptionDetails = new ExceptionDetailsInfo(
                id: 0,
                outerId: -1,
                typeName: "CustomException",
                message: "Method 5: Custom exception details without actual Exception object",
                hasFullStack: true,
                stack: "   at MyNamespace.MyClass.MyMethod()\r\n   at MyNamespace.Program.Main()",
                parsedStack: new[]
                {
                    new Microsoft.ApplicationInsights.DataContracts.StackFrame("MyAssembly", "MyClass.cs", 0, 42, "MyMethod"),
                    new Microsoft.ApplicationInsights.DataContracts.StackFrame("MyAssembly", "Program.cs", 1, 15, "Main")
                });

            var customExceptionTelemetry = new ExceptionTelemetry(
                new[] { customExceptionDetails },
                SeverityLevel.Critical,
                "CustomProblemId",
                new Dictionary<string, string> 
                { 
                    { "Method", "5" }, 
                    { "Custom", "True" } 
                });
            
            telemetryClient.TrackException(customExceptionTelemetry);
        }

        private static void Method6_GlobalProperties(TelemetryClient telemetryClient)
        {
            Console.WriteLine("6. Using Context.GlobalProperties");
            try
            {
                throw new InvalidOperationException("Method 6: Using GlobalProperties");
            }
            catch (Exception ex)
            {
                var exceptionTelemetry = new ExceptionTelemetry(ex);
                exceptionTelemetry.Context.GlobalProperties.Add("Environment", "Development");
                exceptionTelemetry.Context.GlobalProperties.Add("Version", "1.0.0");
                exceptionTelemetry.Properties.Add("Method", "6");
                telemetryClient.TrackException(exceptionTelemetry);
            }
        }

        private static void Method7_ParameterlessConstructor(TelemetryClient telemetryClient)
        {
            Console.WriteLine("7. Parameterless constructor with manual setup");
            
            var manualExceptionTelemetry = new ExceptionTelemetry();
            manualExceptionTelemetry.Exception = new InvalidOperationException("Method 7: Parameterless constructor");
            manualExceptionTelemetry.SeverityLevel = SeverityLevel.Warning;
            manualExceptionTelemetry.Message = "Custom log message";
            manualExceptionTelemetry.ProblemId = "PROBLEM-123";
            manualExceptionTelemetry.Timestamp = DateTimeOffset.UtcNow;
            manualExceptionTelemetry.Properties.Add("Method", "7");
            manualExceptionTelemetry.Properties.Add("Manually", "Created");
            telemetryClient.TrackException(manualExceptionTelemetry);
        }

        private static void Method8_SetParsedStack(TelemetryClient telemetryClient)
        {
            Console.WriteLine("8. Exception with SetParsedStack");
            try
            {
                throw new InvalidOperationException("Method 8: With custom parsed stack");
            }
            catch (Exception ex)
            {
                var exceptionTelemetry = new ExceptionTelemetry(ex);
                exceptionTelemetry.Properties.Add("Method", "8");
                
                // Get the current stack frames and set them
                var stackTrace = new System.Diagnostics.StackTrace(ex, true);
                exceptionTelemetry.SetParsedStack(stackTrace.GetFrames());
                
                telemetryClient.TrackException(exceptionTelemetry);
            }
        }

        private static void Method9_ExceptionReplacement(TelemetryClient telemetryClient)
        {
            Console.WriteLine("9. Exception replacement (null handling)");
            
            var nullSafeExceptionTelemetry = new ExceptionTelemetry(new InvalidOperationException("Method 9: Initial exception"));
            nullSafeExceptionTelemetry.Exception = null; // Can set to null if needed
            nullSafeExceptionTelemetry.Exception = new ArgumentException("Method 9: Exception replacement");
            nullSafeExceptionTelemetry.Properties.Add("Method", "9");
            telemetryClient.TrackException(nullSafeExceptionTelemetry);
        }
    }
}
