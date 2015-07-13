namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Microsoft.ApplicationInsights.DataContracts;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using EndpointOperationContext = Microsoft.Developer.Analytics.DataCollection.Model.v2.OperationContextData;
    using JsonConvert = Newtonsoft.Json.JsonConvert;

    [TestClass]
    public class OperationContextTest
    {
        [TestMethod]
        public void ClassIsPublicToAllowInstantiationInSdkAndUserCode()
        {
            Assert.True(typeof(OperationContext).GetTypeInfo().IsPublic);
        }
        
        [TestMethod]
        public void IdIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            Assert.Null(operation.Id);
        }

        [TestMethod]
        public void NameIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            Assert.Null(operation.Name);
        }

        [TestMethod]
        public void SyntheticSourceIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            Assert.Null(operation.SyntheticSource);
        }

        [TestMethod]
        public void IdCanBeChangedByUserToSupplyApplicationDefinedValue()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            operation.Id = "42";
            Assert.Equal("42", operation.Id);
        }

        [TestMethod]
        public void NameCanBeChangedByUserToSupplyApplicationDefinedValue()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            operation.Name = "SampleOperationName";
            Assert.Equal("SampleOperationName", operation.Name);
        }

        [TestMethod]
        public void SyntheticSourceCanBeChangedByUserToSupplyApplicationDefinedValue()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            operation.SyntheticSource = "Sample";
            Assert.Equal("Sample", operation.SyntheticSource);
        }
    }
}
