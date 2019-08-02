namespace Microsoft.ApplicationInsights.Common.Extensions
{
    using System;
    using System.Reflection;

    internal static class EnumExtensions
    {
        public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
        {
#if NETSTANDARD1_3
            throw new NotImplementedException();
#else
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            return type.GetField(name).GetCustomAttribute<TAttribute>();
#endif
        }
    }
}
