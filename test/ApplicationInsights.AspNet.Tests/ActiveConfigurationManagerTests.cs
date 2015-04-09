namespace Microsoft.ApplicationInsights.AspNet.Tests
{
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.Extensibility;
    using System;
    using Xunit;
    using System.Linq;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.AspNet.Hosting;
    using System.Collections.Generic;

    public class ActiveConfigurationManagerTests
    {
        #region TelemetryInitializers
        [Fact]
        public void AddTelemetryInitializersDoesNotAddAnythingIfProviderNull()
        {
            var config = new TelemetryConfiguration();
            int numberOfInitialzersBefore = config.TelemetryInitializers.Count;

            ActiveConfigurationManager.AddTelemetryInitializers(config, null);

            Assert.Equal(numberOfInitialzersBefore, config.TelemetryInitializers.Count);
        }

        [Fact]
        public void AddTelemetryInitializersDoesNotThrowIfConfigNull()
        {
            ActiveConfigurationManager.AddTelemetryInitializers(null, new TestServiceProvider());
        }

        [Fact]
        public void AddTelemetryInitializersWillAddOperationNameTelelemtryInitializer()
        {
            var config = new TelemetryConfiguration();

            ActiveConfigurationManager.AddTelemetryInitializers(config, new TestServiceProvider(new List<object>() { new HttpContextAccessor()}));

            var items = config.TelemetryInitializers
                .OfType<OperationNameTelemetryInitializer>()
                .ToList();

            Assert.Equal(1, items.Count);
        }

        [Fact]
        public void AddTelemetryInitializersWillAddOperationIdTelelemtryInitializer()
        {
            var config = new TelemetryConfiguration();

            ActiveConfigurationManager.AddTelemetryInitializers(config, new TestServiceProvider(new List<object>() { new HttpContextAccessor() }));

            var items = config.TelemetryInitializers
                    .OfType<OperationIdTelemetryInitializer>()
                    .ToList();

            Assert.Equal(1, items.Count);
        }
        #endregion

        #region ContextInitializers

        [Fact]
        public void AddContextInitializersDoesNotThrowIfConfigNull()
        {
            ActiveConfigurationManager.AddContextInitializers(null);
        }

        [Fact]
        public void AddTelemetryContextsWillAddDomainNameRoleInstanceContextInitializer()
        {
            var config = new TelemetryConfiguration();

            ActiveConfigurationManager.AddContextInitializers(config);

            var items = config.ContextInitializers
                    .OfType<DomainNameRoleInstanceContextInitializer>()
                    .ToList();

            Assert.Equal(1, items.Count);
        }
        #endregion
    }
}