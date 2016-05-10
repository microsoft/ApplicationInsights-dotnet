namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    public class SdkVersionTestUtils
    {
        public static string GetExpectedSdkVersion()
        {
            string expectedSdkVersion;

#if NET451
        expectedSdkVersion = "aspnet5f";
#else
        expectedSdkVersion = "aspnet5c";
#endif
            return expectedSdkVersion;
        }
    }
}
