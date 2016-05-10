namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    public class SdkVersionTestUtils
    {
        public static string GetExpectedSdkVersion()
        {
            string expectedSdkVersion = "aspnet5";

#if NET451
        expectedSdkVersion = expectedSdkVersion + "F";
#else
        expectedSdkVersion = expectedSdkVersion + "C";
#endif
            return expectedSdkVersion;
        }
    }
}
