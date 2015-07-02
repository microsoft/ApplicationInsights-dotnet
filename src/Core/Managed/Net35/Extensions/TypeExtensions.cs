namespace System.Reflection
{
    using System.Collections.Generic;

    internal static class TypeExtensions
    {
        public static TypeInfo GetTypeInfo(this Type type)
        {
            return new TypeInfo(type);
        }

        public static IEnumerable<PropertyInfo> GetRuntimeProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        }
    }
}
