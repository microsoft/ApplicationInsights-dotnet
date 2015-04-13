namespace FunctionalTestUtils
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Framework.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNet.Builder;

    public static class BackTelemetryChannelExtensions
    {
        /// <summary>
        /// I know, statics are bad. Let's refactor when it will become a problem
        /// </summary>
        private static BackTelemetryChannel channel;

        /// <summary>
        /// If applciaiton is running under functional tests - in process channel will be used.
        /// Otherwise - regular channel will be used
        /// </summary>
        /// <param name="services"></param>
        public static void UseFunctionalTestTelemetryChannel(this IApplicationBuilder app)
        {
            channel = new BackTelemetryChannel();

            var telemetryConfiguration = app.ApplicationServices.GetRequiredService<TelemetryConfiguration>();
            telemetryConfiguration.TelemetryChannel = channel;
        }

        /// <summary>
        /// I haven't implemented Reset method to delete channel. Since an applicaiton will either be used by
        /// unit tests or started directly - it should not be a big problem, every unit test will override 
        /// channel with it's own version.
        /// </summary>
        /// <param name="buffer"></param>
        public static void InitializeFunctionalTestTelemetryChannel(IList<ITelemetry> buffer)
        {
            channel.buffer = buffer;
        }
    }
}