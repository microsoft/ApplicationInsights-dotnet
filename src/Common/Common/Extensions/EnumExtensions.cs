namespace Microsoft.ApplicationInsights.Common.Extensions
{
    using System;
    using System.Reflection;

    internal static class EnumExtensions
    {
        public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
        {
#if NETSTANDARD1_3
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            return type.GetRuntimeField(name).GetCustomAttribute<TAttribute>();
#else
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            return type.GetField(name).GetCustomAttribute<TAttribute>();
#endif
        }
    }
}