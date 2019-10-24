namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InstrumentationKeyValidationTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyEmptyIkeyIsInvalid() => RunTest(string.Empty);

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void VerifyNullIkeyIsInvalid() => RunTest(null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyWhitespaceIkeyIsInvalid() => RunTest("   ");

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyExtraQuotesIkeyIsInvalid() => RunTest("\"00000000-0000-0000-0000-000000000000\"");
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Instrumentation Key contains non-printable characters.")]
        public void VerifyNonPrintableCharacterIkeyIsInvalid() => RunTest("00000000-0000-0000-0000-000000000000\r\n");
        
        [TestMethod]
        public void VerifyExample1() => RunTest("AAA-00000000-0000-0000-0000-000000000000");
        
        [TestMethod]
        public void VerifyExample2() => RunTest("00000000-0000-0000-0000-000000000000");
        
        [TestMethod]
        public void VerifyExample3() => RunTest("a:00000000-0000-0000-0000-000000000000");

        private void RunTest(string input) => InstrumentationKeyValidation.Validate(input);
    }
}
