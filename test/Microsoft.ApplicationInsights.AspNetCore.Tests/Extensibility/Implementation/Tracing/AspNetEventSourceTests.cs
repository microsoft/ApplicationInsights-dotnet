//-----------------------------------------------------------------------
// <copyright file="AspNetEventSourceTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.ApplicationInsights.AspNet.Tests.Extensibility.Implementation.Tracing
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Reflection;
    using Xunit;

    /// <summary>
    /// Tests for the AspNetEventSource class.
    /// </summary>
    public class AspNetEventSourceTests
    {
        /// <summary>
        /// Tests the event source methods and their attributes.
        /// </summary>
        [Fact]
        public void TestThatEventSourceMethodsAreImplementedConsistentlyWithTheirAttributes()
        {
            Assembly asm = Assembly.Load(new AssemblyName("Microsoft.ApplicationInsights.AspNet"));
            Type eventSourceType = asm.GetType("Microsoft.ApplicationInsights.AspNet.Extensibility.Implementation.Tracing.AspNetEventSource");
            EventSource aspNetEventSource = (EventSource)eventSourceType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            EventSourceTests.MethodsAreImplementedConsistentlyWithTheirAttributes(aspNetEventSource);
        }
    }
}
