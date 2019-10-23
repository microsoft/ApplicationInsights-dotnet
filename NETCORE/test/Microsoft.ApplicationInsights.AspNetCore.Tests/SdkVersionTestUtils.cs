namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    public class SdkVersionTestUtils
    {
#if NET451 || NET46
        public const string VersionPrefix = "aspnet5f:";
#else
        public const string VersionPrefix = "aspnet5c:";
#endif
    }
}
