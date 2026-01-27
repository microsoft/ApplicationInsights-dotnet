# Troubleshooting

## For Immediate Support 

For immediate support relating to the Application Insights .NET SDK we encourage you to file an [Azure Support Request](https://docs.microsoft.com/azure/azure-portal/supportability/how-to-create-azure-support-request) with Microsoft Azure instead of filing a GitHub Issue in this repository. 
You can do so by going online to the [Azure portal](https://portal.azure.com/) and submitting a support request. Access to subscription management and billing support is included with your Microsoft Azure subscription, and technical support is provided through one of the [Azure Support Plans](https://azure.microsoft.com/support/plans/). For step-by-step guidance for the Azure portal, see [How to create an Azure support request](https://docs.microsoft.com/azure/azure-portal/supportability/how-to-create-azure-support-request). Alternatively, you can create and manage your support tickets programmatically using the [Azure Support ticket REST API](https://docs.microsoft.com/rest/api/support/).

## SDK Internal Logs

### Self-Diagnostics

Application Insights 3.x ships a "self-diagnostics feature" which captures internal events and writes to a log file in a specified directory.

The self-diagnostics feature can be enabled/changed/disabled while the process is running.
The SDK will attempt to read the configuration file every 10 seconds, using a non-exclusive read-only mode.
The SDK will create or overwrite a file with new logs according to the configuration.
This file will not exceed the configured max size and will be circularly overwritten.

#### Configuration

Configuration is controlled by a file named `ApplicationInsightsDiagnostics.json`.
The configuration file must be no more than 4 KiB, otherwise only the first 4 KiB of content will be read.

**To enable self-diagnostics**, go to the [current working directory](https://en.wikipedia.org/wiki/Working_directory) of your process and create a configuration file.
In most cases, you could just drop the file along your application.
On Windows, you can use [Process Explorer](https://docs.microsoft.com/sysinternals/downloads/process-explorer), 
double click on the process to pop up Properties dialog, and find "Current directory" in "Image" tab.
Internally, the SDK looks for the configuration file located in [GetCurrentDirectory](https://docs.microsoft.com/dotnet/api/system.io.directory.getcurrentdirectory),
and then [AppContext.BaseDirectory](https://docs.microsoft.com/dotnet/api/system.appcontext.basedirectory).
You can also find the exact directory by calling these methods from your code.

**To disable self-diagnostics**, delete the configuration file.

Example: 
```json
{
    "LogDirectory": ".",
    "FileSize": 1024,
    "LogLevel": "Error"
}
```

#### Configuration Parameters

A `FileSize`-KiB log file named as `YearMonthDay-HourMinuteSecond.ExecutableName.ProcessId.log` (e.g. `20010101-120000.foobar.exe.12345.log`) will be generated at the specified directory `LogDirectory`.
The file name starts with the `DateTime.UtcNow` timestamp of when the file was created.

1. `LogDirectory` is the directory where the output log file will be stored. 
It can be an absolute path or a relative path to the current directory.

2. `FileSize` is a positive integer, which specifies the log file size in [KiB](https://en.wikipedia.org/wiki/Kibibyte).
This value must be between 1 MiB and 128 MiB (inclusive), or it will be rounded to the closest upper or lower limit.
The log file will never exceed this configured size, and will be circularly rewritten.

3. `LogLevel` is the lowest level of the events to be captured. 
This value must match one of the [fields](https://docs.microsoft.com/dotnet/api/system.diagnostics.tracing.eventlevel#fields) of the `EventLevel` enum.
Lower severity levels encompass higher severity levels (e.g. `Warning` includes the `Error` and `Critical` levels).

**Warning**: If the SDK fails to parse any of these fields, the configuration file will be treated as invalid and self-diagnostics will be disabled.
