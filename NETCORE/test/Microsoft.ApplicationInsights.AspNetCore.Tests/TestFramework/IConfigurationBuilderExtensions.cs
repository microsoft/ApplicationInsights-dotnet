using System.IO;
using System.Text;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.Configuration;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TestFramework
{
    static class IConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddMockJsonWithFileLoggingConfig(this IConfigurationBuilder builder, bool enableDiagnosticsTelemetryModule, bool enableSelfDiagnosticsFileLogging)
        {
            var appSettings = $@"{{
                ""ApplicationInsights"": {{
                    ""{nameof(ApplicationInsightsServiceOptions.InstrumentationKey)}"": ""testIkey"",
                    ""{nameof(ApplicationInsightsServiceOptions.EnableDiagnosticsTelemetryModule)}"": ""{enableDiagnosticsTelemetryModule}"",
                    ""{nameof(ApplicationInsightsServiceOptions.EnableSelfDiagnosticsFileLogging)}"": ""{enableSelfDiagnosticsFileLogging}"",
                    }}
                }}";

            return builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(appSettings)));
        }
    }
}
