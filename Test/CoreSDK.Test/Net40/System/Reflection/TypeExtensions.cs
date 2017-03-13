using System.Collections.Generic;

namespace System.Reflection
{
    internal static class TypeExtensions
    {
        public static TypeInfo GetTypeInfo(this Type type) => 
            new TypeInfo(type);

        public static IEnumerable<PropertyInfo> GetRuntimeProperties(this Type type) => 
            type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
