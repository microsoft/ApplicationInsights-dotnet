namespace Microsoft.ApplicationInsights.Extensibility.Filtering
{
    using System;
    using System.Reflection;

    internal static class PortableHelpers
    {
#if NET40
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }

        public static MethodInfo GetMethodInfo(this Delegate del)
        {
            return del.Method;
        }
#endif
    }
}
