namespace System
{
    using System.Collections.Generic;
    using System.Reflection;

    internal class TypeInfo
    {
        private Type type;

        public TypeInfo(Type type)
        {
            this.type = type;
        }

        public bool IsSealed
        {
            get { return this.type.IsSealed; }
        }

        public bool IsAbstract 
        { 
            get { return this.type.IsAbstract; } 
        }

        public bool IsPublic
        {
            get { return this.type.IsPublic; }
        }

        public Type BaseType
        {
            get { return this.type.BaseType; }
        }

        public PropertyInfo[] DeclaredProperties
        {
            get { return this.type.GetProperties(); }
        }

        public virtual IEnumerable<ConstructorInfo> DeclaredConstructors
        {
            get
            {
                return
                    type.GetConstructors(
                        BindingFlags.NonPublic | 
                        BindingFlags.Public | 
                        BindingFlags.Static | 
                        BindingFlags.Instance | 
                        BindingFlags.DeclaredOnly);
            }
        }

        public bool IsAssignableFrom(TypeInfo otherTypeInfo)
        {
            return this.type.IsAssignableFrom(otherTypeInfo == null ? null : otherTypeInfo.type);
        }

        public MethodInfo GetDeclaredMethod(string name)
        {
            return type.GetMethod(
                name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        public virtual PropertyInfo GetDeclaredProperty(string name)
        {
            return type.GetProperty(
                name, 
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }
    }
}