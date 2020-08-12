using System.IO;
using System.Text;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.Configuration;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TestFramework
{
    static class IConfigurationBuilderExtensions
    {
#if !NET46
        public static IConfigurationBuilder AddMockJsonWithDiagnostics(this IConfigurationBuilder builder, bool enableDiagnosticsTelemetryModule)
        {
            var appSettings = $@"{{
                ""ApplicationInsights"": {{
                    ""{nameof(ApplicationInsightsServiceOptions.InstrumentationKey)}"": ""testIkey"",
                    ""{nameof(ApplicationInsightsServiceOptions.EnableDiagnosticsTelemetryModule)}"": ""{enableDiagnosticsTelemetryModule}"",
                    }}
                }}";

            return builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(appSettings)));
        }
#endif
    }
}
