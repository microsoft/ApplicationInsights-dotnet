namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class TelemetryConfigurationFactory
    {
        private static TelemetryConfigurationFactory instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryConfigurationFactory"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is protected because <see cref="TelemetryConfigurationFactory"/> is only meant to be instantiated 
        /// by the <see cref="Instance"/> property or by tests.
        /// </remarks>
        protected TelemetryConfigurationFactory()
        {
        }

        /// <summary>
        /// Gets or sets the default <see cref="TelemetryConfigurationFactory"/> instance used by <see cref="TelemetryConfiguration"/>.
        /// </summary>
        /// <remarks>
        /// This property is a test isolation "pinch point" that allows us to test <see cref="TelemetryConfiguration"/> without using reflection.
        /// </remarks>
        public static TelemetryConfigurationFactory Instance
        {
            get { return instance ?? (instance = new TelemetryConfigurationFactory()); }
            set { instance = value; }
        }

        protected static object CreateInstance(Type interfaceType, string typeName, object[] constructorArgs = null)
        {
            Type type = GetType(typeName);
            if (type == null)
            {
                CoreEventSource.Log.TypeWasNotFoundConfigurationError(typeName);
                return null;
            }

            object instanceToCreate;
            try
            {
                instanceToCreate = constructorArgs != null ? Activator.CreateInstance(type, constructorArgs) : Activator.CreateInstance(type);
            }
            catch (Exception exp)
            {
                // Ideally we would want to catch MissingMethodException. But there is no such type in PCL.
                CoreEventSource.Log.MissingMethodExceptionConfigurationError(typeName, exp.Message);
                return null;
            }

            if (!interfaceType.IsAssignableFrom(instanceToCreate.GetType()))
            {
                CoreEventSource.Log.IncorrectTypeConfigurationError(type.AssemblyQualifiedName, interfaceType.FullName);
                return null;
            }

            return instanceToCreate;
        }

        private static Type GetType(string typeName)
        {
            Type type = GetManagedType(typeName);
            return type;
        }

        private static Type GetManagedType(string typeName)
        {
            try
            {
                return Type.GetType(typeName);
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
