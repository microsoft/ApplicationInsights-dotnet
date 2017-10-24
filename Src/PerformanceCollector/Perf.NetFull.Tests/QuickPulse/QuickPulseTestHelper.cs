namespace Microsoft.ApplicationInsights.Tests
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Part of the <see cref="QuickPulseTestHelper"/> class for full .NET Framework.
    /// </summary>
    internal static partial class QuickPulseTestHelper
    {
        public static object GetPrivateField(object obj, string fieldName)
        {
            var t = new PrivateObject(obj);
            return t.GetField(fieldName);
        }

        public static void SetPrivateStaticField(Type type, string fieldName, object value)
        {
            PrivateType t = new PrivateType(type);
            t.SetStaticField(fieldName, value);
        }
    }
}