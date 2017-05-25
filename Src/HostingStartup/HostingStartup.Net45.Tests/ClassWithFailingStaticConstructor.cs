namespace Microsoft.ApplicationInsights.HostingStartup.Tests
{
    using System;

    public class ClassWithFailingStaticConstructor
    {
        static ClassWithFailingStaticConstructor()
        {
            throw new Exception("failure from ..ctor");
        }
    }
}