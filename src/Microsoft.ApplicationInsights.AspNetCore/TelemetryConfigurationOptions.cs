using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public class TelemetryConfigurationOptions : IOptions<TelemetryConfiguration>
    {
        public TelemetryConfigurationOptions(IEnumerable<IConfigureOptions<TelemetryConfiguration>> configureOptions)
        {

            Console.WriteLine(Value.TelemetryChannel);
            foreach (var c in configureOptions)
            {
                c.Configure(Value);
            }
        }

        public TelemetryConfiguration Value => TelemetryConfiguration.Active;
    }
}