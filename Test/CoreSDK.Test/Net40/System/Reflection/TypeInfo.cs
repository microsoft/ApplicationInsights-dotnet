using System.Collections.Generic;

namespace System.Reflection
{
    internal class TypeInfo
    {
        public const BindingFlags AllFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private Type type;

        public TypeInfo(Type type)
        {
            this.type = type;
        }

        public bool IsPublic => type.IsPublic;
        public bool IsSealed => type.IsSealed;
        public bool IsGenericType => type.IsGenericType;
        public bool IsAbstract => type.IsAbstract;
        public bool IsValueType => type.IsValueType;
        public bool IsAssignableFrom(TypeInfo typeInfo) => type.IsAssignableFrom(typeInfo.type);
        public Type GetGenericTypeDefinition() => type.GetGenericTypeDefinition();
        public Assembly Assembly => type.Assembly;
        public string Name => type.Name;
        public Type[] GenericTypeArguments => type.GetGenericArguments();

        public IEnumerable<ConstructorInfo> DeclaredConstructors => type.GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        public PropertyInfo GetDeclaredProperty(string name) => type.GetProperty(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        public MethodInfo GetDeclaredMethod(string name) => type.GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
