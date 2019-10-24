namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InstrumentationKeyValidationTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyEmptyIkeyIsInvalid() => InstrumentationKeyValidation.Validate(string.Empty);

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void VerifyNullIkeyIsInvalid() => InstrumentationKeyValidation.Validate(null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyWhitespaceIkeyIsInvalid() => InstrumentationKeyValidation.Validate("   ");

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyExtraQuotesIkeyIsInvalid() => InstrumentationKeyValidation.Validate("\"00000000-0000-0000-0000-000000000000\"");
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Instrumentation Key contains non-printable characters.")]
        public void VerifyNonPrintableCharacterIkeyIsInvalid() => InstrumentationKeyValidation.Validate("00000000-0000-0000-0000-000000000000\r\n");
        
        [TestMethod]
        public void VerifyExample1() => InstrumentationKeyValidation.Validate("AAA-00000000-0000-0000-0000-000000000000");
        
        [TestMethod]
        public void VerifyExample2() => InstrumentationKeyValidation.Validate("00000000-0000-0000-0000-000000000000");
        
        [TestMethod]
        public void VerifyExample3() => InstrumentationKeyValidation.Validate("a:00000000-0000-0000-0000-000000000000");
    }
}
