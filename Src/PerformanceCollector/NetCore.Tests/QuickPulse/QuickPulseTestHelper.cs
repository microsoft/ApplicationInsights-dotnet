namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Reflection;

    internal static partial class QuickPulseTestHelper
    {
        public static object GetPrivateField(object obj, string fieldName)
        {
            TypeInfo type = obj.GetType().GetTypeInfo();
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field.GetValue(obj);
        }

        public static void SetPrivateStaticField(Type type, string fieldName, object value)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            FieldInfo field = typeInfo.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, value);
        }
    }
}