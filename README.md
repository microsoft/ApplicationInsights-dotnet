Application Insights logging adaptors. 
==============================

If you use NLog, log4Net or System.Diagnostics.Trace for diagnostic tracing in your ASP.NET application, you can have your logs sent to Visual Studio Application Insights, where you can explore and search them. Your logs will be merged with the other telemetry coming from your application, so that you can identify the traces associated with servicing each user request, and correlate them with other events and exception reports.

Read more [here](https://azure.microsoft.com/en-us/documentation/articles/app-insights-search-diagnostic-logs/#trace).

##Nuget packages

- [For NLog](http://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/).
- [For Log4Net](http://www.nuget.org/packages/Microsoft.ApplicationInsights.Log4NetAppender/)
- [For System.Diagnostics](http://www.nuget.org/packages/Microsoft.ApplicationInsights.TraceListener/)





