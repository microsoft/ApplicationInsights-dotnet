namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    public class SdkVersionTestUtils
    {
        public static string GetExpectedSdkVersion()
        {
            string expectedSdkVersion;

#if NET451
        expectedSdkVersion = "aspnet5F";
#else
        expectedSdkVersion = "aspnet5C";
#endif
            return expectedSdkVersion;
        }
    }
}
