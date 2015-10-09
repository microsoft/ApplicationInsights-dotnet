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
            Assert.Null(operation.RootName);
        }

        [TestMethod]
        public void SyntheticSourceIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            Assert.Null(operation.SyntheticSource);
        }

        [TestMethod]
        public void ParentIdIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            Assert.Null(operation.ParentId);
        }

        [TestMethod]
        public void RootIdIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            Assert.Null(operation.RootId);
        }

        [TestMethod]
        public void CorrelationVectorIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            Assert.Null(operation.CorrelationVector);
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
            operation.RootName = "SampleOperationName";
            Assert.Equal("SampleOperationName", operation.RootName);
        }

        [TestMethod]
        public void SyntheticSourceCanBeChangedByUserToSupplyApplicationDefinedValue()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            operation.SyntheticSource = "Sample";
            Assert.Equal("Sample", operation.SyntheticSource);
        }

        [TestMethod]
        public void ParentIdCanBeChangedByUserToSupplyApplicationDefinedValue()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            operation.ParentId = "ParentId";
            Assert.Equal("ParentId", operation.ParentId);
        }

        [TestMethod]
        public void RootIdCanBeChangedByUserToSupplyApplicationDefinedValue()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            operation.RootId = "RootId";
            Assert.Equal("RootId", operation.RootId);
        }

        [TestMethod]
        public void CorrelationVectorCanBeChangedByUserToSupplyApplicationDefinedValue()
        {
            var operation = new OperationContext(new Dictionary<string, string>());
            operation.CorrelationVector = "CorrelationVector";
            Assert.Equal("CorrelationVector", operation.CorrelationVector);
        }
    }
}
