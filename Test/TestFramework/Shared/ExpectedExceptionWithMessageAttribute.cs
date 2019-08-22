namespace Microsoft.ApplicationInsights.TestFramework
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;


    /// <summary>
    /// Extension of <see cref="ExpectedExceptionAttribute"/> class to validate a test based on both the exception type and message.
    /// </summary>
    /// <remarks>
    /// Inspired by StackOverflow answer: https://stackoverflow.com/a/16945443/1466768
    /// </remarks>
    public class ExpectedExceptionWithMessageAttribute : ExpectedExceptionBaseAttribute
    {
        private readonly Type exceptionType;
        private readonly string expectedMessage;

        public ExpectedExceptionWithMessageAttribute(Type exceptionType) : this(exceptionType, null)
        {
        }

        public ExpectedExceptionWithMessageAttribute(Type exceptionType, string expectedMessage)
        {
            this.exceptionType = exceptionType;
            this.expectedMessage = expectedMessage;
        }

        protected override void Verify(Exception ex)
        {
            if (ex.GetType() != this.exceptionType)
            {
                Assert.Fail($"Test method threw exception {this.exceptionType.FullName}, but exception {ex.GetType().FullName} was expected. Exception message: {ex.Message}");
            }

            if (this.expectedMessage != null && this.expectedMessage != ex.Message)
            {
                Assert.Fail($"Test method threw the expected exception type, but with an unexpected message: {ex.Message}");
            }

            Console.Write("ExpectedExceptionWithMessageAttribute:" + ex.Message);
        }
    }
}
