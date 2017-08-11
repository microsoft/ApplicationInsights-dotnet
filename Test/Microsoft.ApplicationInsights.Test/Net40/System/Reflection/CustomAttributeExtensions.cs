namespace System.Reflection
{
    internal static class CustomAttributeExtensions
    {
        public static Attribute GetCustomAttribute(this MemberInfo element, Type attributeType) => Attribute.GetCustomAttribute(element, attributeType);

        public static T GetCustomAttribute<T>(this MemberInfo element) where T : Attribute => (T)(element.GetCustomAttribute(typeof(T)));
    }
}
