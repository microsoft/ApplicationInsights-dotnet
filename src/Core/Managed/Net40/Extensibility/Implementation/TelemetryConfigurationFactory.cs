namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;

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

        public virtual void Initialize(TelemetryConfiguration configuration)
        {
            configuration.TelemetryInitializers.Add(new SdkVersionPropertyTelemetryInitializer());
            configuration.TelemetryInitializers.Add(new TimestampPropertyInitializer());

            // Load customizations from the ApplicationsInsights.config file
            string text = PlatformSingleton.Current.ReadConfigurationXml();
            if (!string.IsNullOrEmpty(text))
            {
                XDocument xml = XDocument.Parse(text);
                LoadFromXml(configuration, xml);
            }
            
            // Creating the default channel if no channel configuration supplied
            configuration.TelemetryChannel = configuration.TelemetryChannel ?? new InMemoryChannel();

            // Creating the the processor chain with default processor (transmissionprocessor) if none configured
            if (configuration.TelemetryProcessors == null)
            {
                configuration.GetTelemetryProcessorChainBuilder().Build();
            }                

            InitializeComponents(configuration);
        }

        protected static object CreateInstance(Type interfaceType, string typeName, object[] constructorArgs = null)
        {
            Type type = GetType(typeName);
            if (type == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Type '{0}' could not be loaded.", typeName));
            }

            object instance = null;
            if (constructorArgs != null)
            {
                instance = Activator.CreateInstance(type, constructorArgs);
            }
            else
            {
                instance = Activator.CreateInstance(type);
            }            

            if (!interfaceType.IsAssignableFrom(instance.GetType()))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Type '{0}' does not implement the required interface {1}.",
                        type.AssemblyQualifiedName,
                        interfaceType.FullName));
            }

            return instance;
        }

        protected static void LoadFromXml(TelemetryConfiguration configuration, XDocument xml)
        {
            XElement applicationInsights = xml.Element(XmlNamespace + "ApplicationInsights");
            LoadInstance(applicationInsights, typeof(TelemetryConfiguration), configuration);
        }

        protected static object LoadInstance(XElement definition, Type expectedType, object instance, object[] constructorArgs = null)
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
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "'{0}' element does not have a Type attribute, does not specify a value and is not a valid collection type",
                            definition.Name.LocalName));
                }

                if (instance != null)
                {
                    LoadProperties(definition, instance);
                    Type elementType;
                    if (GetCollectionElementType(instance.GetType(), out elementType))
                    {
                        MethodInfo genericLoadInstances = LoadInstancesDefinition.MakeGenericMethod(elementType);
                        genericLoadInstances.Invoke(null, new object[] { definition, instance });
                    }
                }
            }

            return instance;
        }

        protected static void BuildTelemetryProcessorChain(XElement definition, TelemetryConfiguration telemetryConfiguration)
        {            
            TelemetryProcessorChainBuilder builder = telemetryConfiguration.GetTelemetryProcessorChainBuilder();
            if (definition != null)
            {
                IEnumerable<XElement> elems = definition.Elements(XmlNamespace + AddElementName);                
                foreach (XElement addElement in elems)
                {
                    builder = builder.Use((current) => 
                    {
                        var constructorArgs = new object[] { current };
                        var instance = LoadInstance(addElement, typeof(ITelemetryProcessor), telemetryConfiguration, constructorArgs);
                        return (ITelemetryProcessor)instance;
                    });                           
                }                
            }

            builder.Build();
        }

        protected static void LoadInstances<T>(XElement definition, ICollection<T> instances)
        {
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

                    bool isNewInstance = instance == null;
                    instance = LoadInstance(addElement, typeof(T), instance);
                    if (isNewInstance)
                    {
                        instances.Add((T)instance);
                    }
                }
            }
        }

        protected static void LoadProperties(XElement instanceDefinition, object instance)
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
                            propertyValue = LoadInstance(propertyDefinition, property.PropertyType, propertyValue);
                            if (property.CanWrite)
                            {
                                property.SetValue(instance, propertyValue, null);
                            }
                        }                        
                    }                    
                    else if (propertyName == "TelemetryModules")
                    {
                        LoadInstance(propertyDefinition, TelemetryModules.Instance.Modules.GetType(), TelemetryModules.Instance.Modules);
                    }
                    else if (instance is TelemetryConfiguration)
                    {
                        continue; // because Status Monitor, VS Tooling can define their own configuration sections we don't care about here.
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                "'{0}' is not a valid property name for type {1}.",
                                propertyName,
                                instanceType.AssemblyQualifiedName));
                    }
                }
            }
        }        

        private static void InitializeComponents(TelemetryConfiguration configuration)
        {
            InitializeComponent(configuration.TelemetryChannel, configuration);            
            InitializeComponents(configuration.TelemetryInitializers, configuration);
            InitializeComponents(configuration.TelemetryProcessors.TelemetryProcessors, configuration);
            InitializeComponents(TelemetryModules.Instance.Modules, configuration);
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
                configurable.Initialize(configuration);
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
                if (valueString == null || valueString == "null")
                {
                    instance = null;
                }
                else if (expectedType == typeof(TimeSpan))
                {
#if NET35
                    instance = TimeSpan.Parse(valueString);
#else
                    instance = TimeSpan.Parse(valueString, CultureInfo.InvariantCulture);
#endif
                }
                else
                {
                    instance = Convert.ChangeType(valueString, expectedType, CultureInfo.InvariantCulture);
                }
            }
            catch (InvalidCastException e)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "'{0}' element has unexpected contents: '{1}'.", definition.Name.LocalName, definition.Value),
                    e);
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