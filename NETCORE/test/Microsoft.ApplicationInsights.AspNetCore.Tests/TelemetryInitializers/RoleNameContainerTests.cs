using System;

using Microsoft.ApplicationInsights.AspNetCore.Implementation;
using Microsoft.AspNetCore.Http;

using Xunit;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    [Trait("Trait", "RoleName")]
    public class RoleNameContainerTests : IDisposable
    {
        public RoleNameContainerTests()
        {
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "initialize");
        }

        [Fact]
        public void VerifyCanSetRoleNameFromEnvironmentVariable()
        {
            try
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "a.b.c.azurewebsites.net");
                RoleNameContainer.HostNameSuffix = ".azurewebsites.net";
                RoleNameContainer.SetFromEnvironmentVariable(out bool ignore);

                Assert.Equal("a.b.c", RoleNameContainer.RoleName);
            }
            finally
            {
                this.ClearEnvironmentVariable();
            }
        }

        [Fact]
        public void VerifyCanSetRoleNameFromHeaders()
        {
            RoleNameContainer.HostNameSuffix = ".azurewebsites.net";

            var headers = new HeaderDictionary();
            headers.Add("WAS-DEFAULT-HOSTNAME", "d.e.f.azurewebsites.net");
            RoleNameContainer.Set(headers);

            Assert.Equal("d.e.f", RoleNameContainer.RoleName);
        }

        [Fact]
        public void VerifyEmptyHeadersDoNotOverwriteEnvironmentVaribaleValue()
        {
            try
            {
                Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", "a.b.c.azurewebsites.net");
                RoleNameContainer.HostNameSuffix = ".azurewebsites.net";
                RoleNameContainer.SetFromEnvironmentVariable(out bool ignore);

                Assert.Equal("a.b.c", RoleNameContainer.RoleName);

                RoleNameContainer.Set(new HeaderDictionary()); // empty headers

                Assert.Equal("a.b.c", RoleNameContainer.RoleName);
            }
            finally
            {
                this.ClearEnvironmentVariable();
            }
        }


        public void Dispose() => this.ClearEnvironmentVariable();

        private void ClearEnvironmentVariable() => Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", null);
    }
}
