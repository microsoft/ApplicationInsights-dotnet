namespace Functional
{
    using Functional.Helpers;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class ExceptionTelemetryTestBase : SingleWebHostTestBase
    {
        protected void ValidateExceptionTelemetry(TelemetryItem<ExceptionData> exceptionTelemetry, TelemetryItem<RequestData> request, int expectedExceptionsCount)
        {
            Assert.AreEqual(this.Config.IKey, exceptionTelemetry.IKey, "iKey is not the same as in config file for exception");
            Assert.AreEqual(request.OperationContext.Id, exceptionTelemetry.OperationContext.Id, "Operation id is incorrect");

            Assert.AreEqual("Platform", exceptionTelemetry.Data.BaseData.HandledAt, "handledAt is incorrect");

            Assert.AreEqual(expectedExceptionsCount, exceptionTelemetry.Data.BaseData.Exceptions.Count, "Exceptions count is incorrect");

            Assert.IsNotNull(exceptionTelemetry.DeviceContext, "Device was not collected");
        }

        protected void ValidateExceptionDetails(
            ExceptionDetails exceptionDetails0,
            string expectedTypeName,
            string expectedMessage,
            string expectedMethod,
            string expectedAssembly,
            int expectedMaxStackPointCount)
        {
            Assert.AreEqual(expectedTypeName, exceptionDetails0.TypeName, "exception type 0 name is incorrect");
            
            Assert.AreEqual(expectedMessage, exceptionDetails0.Message, "message 1 is incorrect");

            Assert.IsTrue(exceptionDetails0.HasFullStack, "'hasFullStack' for type 0 is incorrect");
            
            Assert.IsTrue(exceptionDetails0.ParsedStack.Count > 0, "no stackpoints");
            Assert.IsTrue(exceptionDetails0.ParsedStack.Count <= expectedMaxStackPointCount, "too many stackpoints");

            var firstStackPoint = exceptionDetails0.ParsedStack[0];
            Assert.AreEqual(0, firstStackPoint.Level, "stackpoint level is incorrect");
            Assert.AreEqual(expectedMethod, firstStackPoint.Method, "stackpoint method is incorrect");
            Assert.IsTrue(firstStackPoint.Assembly.StartsWith(expectedAssembly), "Assembly name is incorrect");
        }
    }
}
