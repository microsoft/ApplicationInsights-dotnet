namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class TelemetryConfigurationFactory
    {
        private const string AddElementName = "Add";
        private const string TypeAttributeName = "Type";
        private static readonly MethodInfo LoadInstancesDefinition = typeof(TelemetryConfigurationFactory).GetRuntimeMethods().First(m => m.Name == "LoadInstances");
        private static readonly XNamespace XmlNamespace = "http://schemas.microsoft.com/ApplicationInsights/2013/Settings";

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

        public virtual void Initialize(TelemetryConfiguration configuration, TelemetryModules modules, string serializedConfiguration)
        {
            if (modules != null)
            {
                // Create diagnostics module so configuration loading errors are reported to the portal    
                modules.Modules.Add(new DiagnosticsTelemetryModule());
            }

#if !CORE_PCL
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
#endif
            // Load configuration from the specified configuration
            if (!string.IsNullOrEmpty(serializedConfiguration))
            {
                try
                {
                    XDocument xml = XDocument.Parse(serializedConfiguration);
                    LoadFromXml(configuration, modules, xml);
                }
                catch (XmlException exp)
                {
                    CoreEventSource.Log.ConfigurationFileCouldNotBeParsedError(exp.Message);
                }
            }

            // Creating the default channel if no channel configuration supplied
            configuration.TelemetryChannel = configuration.TelemetryChannel ?? new InMemoryChannel();

            // Creating the the processor chain with default processor (transmissionprocessor) if none configured
            if (configuration.TelemetryProcessors == null)
            {
                configuration.TelemetryProcessorChainBuilder.Build();
            }

            InitializeComponents(configuration, modules);
        }

        public virtual void Initialize(TelemetryConfiguration configuration, TelemetryModules modules)
        {
            // Load customizations from the ApplicationsInsights.config file
            this.Initialize(configuration, modules, PlatformSingleton.Current.ReadConfigurationXml());
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

        protected static void LoadFromXml(TelemetryConfiguration configuration, TelemetryModules modules, XDocument xml)
        {
            XElement applicationInsights = xml.Element(XmlNamespace + "ApplicationInsights");
            LoadInstance(applicationInsights, typeof(TelemetryConfiguration), configuration, null, modules);
        }

        protected static object LoadInstance(XElement definition, Type expectedType, object instance, object[] constructorArgs, TelemetryModules modules)
        {
            if (definition != null)
            {
                XAttribute typeName = definition.Attribute(TypeAttributeName);
                if (typeName != null)
                {
                    // Type attribute is specified, instantiate a new object of that type
                    // If configuration instance is already created with the correct type, don't create it just load its properties
                    if (instance == null || instance.GetType() != GetType(typeName.Value))
                    {
                        // Type specified, create a new instance                        
                        instance = CreateInstance(expectedType, typeName.Value, constructorArgs);
                    }
                }
                else if (!definition.Elements().Any() && !definition.Attributes().Any())
                {
                    // Type attribute is not specified and no child elements or attributes exist, so this must be a scalar value
                    LoadInstanceFromValue(definition, expectedType, ref instance);
                }
                else if (instance == null && !expectedType.IsAbstract())
                {
                    instance = Activator.CreateInstance(expectedType);
                }
                else if (instance == null)
                {
                    CoreEventSource.Log.IncorrectInstanceAtributesConfigurationError(definition.Name.LocalName);
                }

                if (instance != null)
                {
                    LoadProperties(definition, instance, modules);
                    Type elementType;
                    if (GetCollectionElementType(instance.GetType(), out elementType))
                    {
                        MethodInfo genericLoadInstances = LoadInstancesDefinition.MakeGenericMethod(elementType);
                        genericLoadInstances.Invoke(null, new[] { definition, instance, modules });
                    }
                }
            }

            return instance;
        }

        protected static void BuildTelemetryProcessorChain(XElement definition, TelemetryConfiguration telemetryConfiguration)
        {
            TelemetryProcessorChainBuilder builder = telemetryConfiguration.TelemetryProcessorChainBuilder;
            if (definition != null)
            {
                IEnumerable<XElement> elems = definition.Elements(XmlNamespace + AddElementName);
                foreach (XElement addElement in elems)
                {
                    builder = builder.Use(current =>
                    {
                        var constructorArgs = new object[] { current };
                        return (ITelemetryProcessor)LoadInstance(addElement, typeof(ITelemetryProcessor), telemetryConfiguration, constructorArgs, null);
                    });
                }
            }

            builder.Build();
        }

        protected static void LoadInstances<T>(XElement definition, ICollection<T> instances, TelemetryModules modules)
        {
            // This method is invoked through reflection. Do not delete.
            if (definition != null)
            {
                foreach (XElement addElement in definition.Elements(XmlNamespace + AddElementName))
                {
                    object instance = null;
                    XAttribute typeName = addElement.Attribute(TypeAttributeName);
                    if (typeName != null)
                    {
                        // It is possible that configuration item of that type is already initialized, in that case we don't need to create it again.
                        Type type = GetType(typeName.Value);
                        instance = instances.FirstOrDefault(i => i.GetType() == type);
                    }

                    if (instance == null)
                    {
                        instance = LoadInstance(addElement, typeof(T), instance, null, modules);
                        if (instance != null)
                        {
                            instances.Add((T)instance);
                        }
                    }
                    else
                    {
                        // Apply configuration overrides to element created in code
                        LoadProperties(addElement, instance, null);
                    }
                }
            }
        }

        protected static void LoadProperties(XElement instanceDefinition, object instance, TelemetryModules modules)
        {
            List<XElement> propertyDefinitions = GetPropertyDefinitions(instanceDefinition).ToList();
            if (propertyDefinitions.Count > 0)
            {
                Type instanceType = instance.GetType();
                Dictionary<string, PropertyInfo> properties = instanceType.GetProperties().ToDictionary(p => p.Name);
                foreach (XElement propertyDefinition in propertyDefinitions)
                {
                    string propertyName = propertyDefinition.Name.LocalName;
                    PropertyInfo property;
                    if (properties.TryGetValue(propertyName, out property))
                    {
                        if (propertyName == "TelemetryProcessors")
                        {
                            BuildTelemetryProcessorChain(propertyDefinition, (TelemetryConfiguration)instance);
                        }
                        else
                        {
                            object propertyValue = property.GetValue(instance, null);
                            propertyValue = LoadInstance(propertyDefinition, property.PropertyType, propertyValue, null, modules);
                            if (propertyValue != null && property.CanWrite)
                            {
                                property.SetValue(instance, propertyValue, null);
                            }
                        }
                    }
                    else if (modules != null && propertyName == "TelemetryModules")
                    {
                        LoadInstance(propertyDefinition, modules.Modules.GetType(), modules.Modules, null, modules);
                    }
                    else if (instance is TelemetryConfiguration)
                    {
                        continue; // because Status Monitor, VS Tooling can define their own configuration sections we don't care about here.
                    }
                    else
                    {
                        CoreEventSource.Log.IncorrectPropertyConfigurationError(instanceType.AssemblyQualifiedName, propertyName);
                    }
                }
            }
        }

        private static void InitializeComponents(TelemetryConfiguration configuration, TelemetryModules modules)
        {
            InitializeComponent(configuration.TelemetryChannel, configuration);
            InitializeComponents(configuration.TelemetryInitializers, configuration);
            InitializeComponents(configuration.TelemetryProcessorChain.TelemetryProcessors, configuration);

            if (modules != null)
            {
                InitializeComponents(modules.Modules, configuration);
            }
        }

        private static void InitializeComponents(IEnumerable components, TelemetryConfiguration configuration)
        {
            foreach (object component in components)
            {
                InitializeComponent(component, configuration);
            }
        }

        private static void InitializeComponent(object component, TelemetryConfiguration configuration)
        {
            var configurable = component as ITelemetryModule;
            if (configurable != null)
            {
                try
                {
                    configurable.Initialize(configuration);
                }
                catch (Exception exp)
                {
                    CoreEventSource.Log.ComponentInitializationConfigurationError(component.ToString(), exp.ToInvariantString());
                }
            }
        }

        private static void LoadInstanceFromValue(XElement definition, Type expectedType, ref object instance)
        {
            // Return default value of the expectedType if the xml element is empty
            if (string.IsNullOrEmpty(definition.Value))
            {
                instance = typeof(ValueType).IsAssignableFrom(expectedType) ? Activator.CreateInstance(expectedType) : null;
                return;
            }

            try
            {
                string valueString = definition.Value.Trim();
                expectedType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;
                if (valueString == "null")
                {
                    instance = null;
                }
                else if (expectedType == typeof(TimeSpan))
                {
                    instance = TimeSpan.Parse(valueString, CultureInfo.InvariantCulture);
                }
                else
                {
                    instance = Convert.ChangeType(valueString, expectedType, CultureInfo.InvariantCulture);
                }
            }
            catch (InvalidCastException e)
            {
                CoreEventSource.Log.LoadInstanceFromValueConfigurationError(definition.Name.LocalName, definition.Value, e.Message);
            }
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

        private static bool GetCollectionElementType(Type type, out Type elementType)
        {
            Type collectionType = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType() && i.GetGenericTypeDefinition() == typeof(ICollection<>));
            elementType = collectionType != null ? collectionType.GetGenericArguments()[0] : null;
            return elementType != null;
        }

        private static IEnumerable<XElement> GetPropertyDefinitions(XElement instanceDefinition)
        {
            IEnumerable<XElement> attributeDefinitions = instanceDefinition.Attributes()
                .Where(a => !a.IsNamespaceDeclaration && a.Name.LocalName != TypeAttributeName)
                .Select(a => new XElement(a.Name, a.Value));

            IEnumerable<XElement> elementDefinitions = instanceDefinition.Elements()
                .Where(e => e.Name.LocalName != AddElementName);

            return attributeDefinitions.Concat(elementDefinitions);
        }
    }
}