namespace Functional
{
    using Functional.Helpers;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;

    public class ExceptionTelemetryTestBase : SingleWebHostTestBase
    {
        protected void ValidateExceptionTelemetry(TelemetryItem<ExceptionData> exceptionTelemetry, TelemetryItem<RequestData> request, int expectedExceptionsCount)
        {
            Assert.AreEqual(this.Config.IKey, exceptionTelemetry.iKey, "iKey is not the same as in config file for exception");
            Assert.AreEqual(request.tags[new ContextTagKeys().OperationId], exceptionTelemetry.tags[new ContextTagKeys().OperationId], "Operation id is incorrect");

            Assert.AreEqual(expectedExceptionsCount, exceptionTelemetry.data.baseData.exceptions.Count, "Exceptions count is incorrect");

            Assert.IsTrue(exceptionTelemetry.tags.Where((x)=> { return x.Key.StartsWith("ai.cloud"); }).Count() > 0, "Cloud was not collected");
        }

        protected void ValidateExceptionDetails(
            ExceptionDetails exceptionDetails0,
            string expectedTypeName,
            string expectedMessage,
            string expectedMethod,
            string expectedAssembly,
            int expectedMaxStackPointCount)
        {
            Assert.AreEqual(expectedTypeName, exceptionDetails0.typeName, "exception type 0 name is incorrect");
            
            Assert.AreEqual(expectedMessage, exceptionDetails0.message, "message 1 is incorrect");

            Assert.IsTrue(exceptionDetails0.hasFullStack, "'hasFullStack' for type 0 is incorrect");
            
            Assert.IsTrue(exceptionDetails0.parsedStack.Count > 0, "no stackpoints");
            Assert.IsTrue(exceptionDetails0.parsedStack.Count <= expectedMaxStackPointCount, "too many stackpoints");

            var firstStackPoint = exceptionDetails0.parsedStack[0];
            Assert.AreEqual(0, firstStackPoint.level, "stackpoint level is incorrect");
            Assert.AreEqual(expectedMethod, firstStackPoint.method, "stackpoint method is incorrect");
            Assert.IsTrue(firstStackPoint.assembly.StartsWith(expectedAssembly), "Assembly name is incorrect");
        }
    }
}
