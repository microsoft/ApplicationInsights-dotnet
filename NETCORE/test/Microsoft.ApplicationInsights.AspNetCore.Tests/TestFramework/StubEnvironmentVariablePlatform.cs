
using System.Collections.Generic;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TestFramework
{
    internal class StubEnvironmentVariablePlatform : IPlatform
    {
        private readonly Dictionary<string, string> environmentVariables = new Dictionary<string, string>();

        public void SetEnvironmentVariable(string name, string value) => this.environmentVariables.Add(name, value);

        public bool TryGetEnvironmentVariable(string name, out string value) => this.environmentVariables.TryGetValue(name, out value);

        public string ReadConfigurationXml() => null;

        public IDebugOutput GetDebugOutput() => new FakeDebugOutput();

        public string GetMachineName() => nameof(StubEnvironmentVariablePlatform);
    }
}
