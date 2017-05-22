namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    /// <summary>
    /// Portable tests for <see cref="UserContext"/>.
    /// </summary>
    [TestClass]
    public class UserContextTest
    {
        [TestMethod]
        public void ClassIsPublicToAllowSpecifyingCustomUserContextPropertiesInUserCode()
        {
            Assert.True(typeof(UserContext).GetTypeInfo().IsPublic);
        }
        
        [TestMethod]
        public void IdCanBeChangedByUserToSpecifyACustomValue()
        {
            var context = new UserContext();
            context.Id = "test value";
            Assert.Equal("test value", context.Id);
        }

        [TestMethod]
        public void UserAgentIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var context = new UserContext();
            Assert.Null(context.UserAgent);
        }

        [TestMethod]
        public void UserAgentCanBeChangedByUserToSpecifyACustomValue()
        {
            var context = new UserContext();
            context.UserAgent = "test value";
            Assert.Equal("test value", context.UserAgent);
        }

        [TestMethod]
        public void AccountIdIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var context = new UserContext();
            Assert.Null(context.AccountId);
        }

        [TestMethod]
        public void AccountIdCanBeChangedByUserToSpecifyACustomValue()
        {
            var context = new UserContext();
            context.AccountId = "test value";
            Assert.Equal("test value", context.AccountId);
        }
    }
}
