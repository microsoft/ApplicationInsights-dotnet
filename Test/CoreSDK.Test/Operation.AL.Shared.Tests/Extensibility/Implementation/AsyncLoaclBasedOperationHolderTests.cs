namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests corresponding to TelemetryClientExtension methods.
    /// </summary>
    [TestClass]
    public class AsyncLoaclBasedOperationHolderTests
    {
        /// <summary>
        /// Tests the scenario if OperationItem throws ArgumentNullException with null telemetry client.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreatingOperationItemWithNullTelemetryClientThrowsArgumentNullException()
        {
            var operationItem = new OperationHolder<DependencyTelemetry>(null, new DependencyTelemetry());
        }

        /// <summary>
        /// Tests the scenario if OperationItem throws ArgumentNullException with null telemetry.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreatingOperationItemWithNullTelemetryThrowsArgumentNullException()
        {
            var operationItem = new OperationHolder<DependencyTelemetry>(new TelemetryClient(), null);
        }

        /// <summary>
        /// Tests the scenario if creating OperationItem does not throw exception if arguments are not null.
        /// </summary>
        [TestMethod]
        public void CreatingOperationItemDoesNotThrowOnPassingValidArguments()
        {
            var operationItem = new OperationHolder<DependencyTelemetry>(new TelemetryClient(), new DependencyTelemetry());
        }
    }
}
