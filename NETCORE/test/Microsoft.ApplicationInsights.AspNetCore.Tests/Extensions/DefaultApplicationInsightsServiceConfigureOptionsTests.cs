namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;

    using Xunit;

    /// <summary>
    /// Because <see cref="DefaultApplicationInsightsServiceConfigureOptions"/> now has two constructors,
    /// these tests verify that dependency injection works as expected.
    /// </summary>
    public class DefaultApplicationInsightsServiceConfigureOptionsTests
    {
        [Fact]
        public void Verify_IHostingEnvironment()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var mockHostingEnvironment = new Moq.Mock<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();

            mockHostingEnvironment.Setup(x => x.ContentRootPath).Returns(Directory.GetCurrentDirectory());

            var services = new ServiceCollection()
                .AddSingleton<Microsoft.AspNetCore.Hosting.IHostingEnvironment>(mockHostingEnvironment.Object);

            services.TryAddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>, DefaultApplicationInsightsServiceConfigureOptions>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var test = serviceProvider.GetService<IConfigureOptions<ApplicationInsightsServiceOptions>>();

            Assert.NotNull(test);
#pragma warning restore CS0618 // Type or member is obsolete
        }

#if NETCOREAPP
        [Fact]
        public void Verify_IHostEnvironment()
        {
            var mockHostEnvironment = new Moq.Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
            mockHostEnvironment.Setup(x => x.ContentRootPath).Returns(Directory.GetCurrentDirectory());

            var services = new ServiceCollection()
                .AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(mockHostEnvironment.Object);

            services.TryAddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>, DefaultApplicationInsightsServiceConfigureOptions>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var test = serviceProvider.GetService<IConfigureOptions<ApplicationInsightsServiceOptions>>();

            Assert.NotNull(test);
        }

        [Fact]
        public void Verify_Both()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var mockHostingEnvironment = new Moq.Mock<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();
            mockHostingEnvironment.Setup(x => x.ContentRootPath).Returns(Directory.GetCurrentDirectory());

            var mockHostEnvironment = new Moq.Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
            mockHostEnvironment.Setup(x => x.ContentRootPath).Returns(Directory.GetCurrentDirectory());

            var services = new ServiceCollection()
                .AddSingleton<Microsoft.AspNetCore.Hosting.IHostingEnvironment>(mockHostingEnvironment.Object)
                .AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(mockHostEnvironment.Object);

            services.TryAddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>, DefaultApplicationInsightsServiceConfigureOptions>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var test = serviceProvider.GetService<IConfigureOptions<ApplicationInsightsServiceOptions>>();

            Assert.NotNull(test);
#pragma warning restore CS0618 // Type or member is obsolete
        }
#endif
    }
}
