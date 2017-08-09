namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    /// <summary>
    /// Tests of exception stack serialization.
    /// </summary>
    [TestClass]
    public class ExceptionConverterTest
    {
        [TestMethod]
        public void CallingConvertToExceptionDetailsWithNullExceptionThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ExceptionConverter.ConvertToExceptionDetails(null, null));
        }

        [TestMethod]
        public void EmptyStringIsReturnedForExceptionWithoutStack()
        {
            var exp = new ArgumentException();

            ExceptionDetails expDetails = ExceptionConverter.ConvertToExceptionDetails(exp, null);

            Assert.Equal(string.Empty, expDetails.stack);
            Assert.Equal(0, expDetails.parsedStack.Count);

            // hasFullStack defaults to true.
            Assert.True(expDetails.hasFullStack);
        }

        [TestMethod]
        public void AllStackFramesAreConvertedIfSizeOfParsedStackIsLessOrEqualToMaximum()
        {
            var exp = this.CreateException(42);

            ExceptionDetails expDetails = ExceptionConverter.ConvertToExceptionDetails(exp, null);
            Assert.Equal(43, expDetails.parsedStack.Count);
            Assert.True(expDetails.hasFullStack);
        }

        [TestMethod]
        public void TestFirstAndLastStackPointsAreCollectedForLongStack()
        {
            var exp = this.CreateException(300);

            ExceptionDetails expDetails = ExceptionConverter.ConvertToExceptionDetails(exp, null);
            
            Assert.False(expDetails.hasFullStack);
            Assert.True(expDetails.parsedStack.Count < 300);

            // We should keep top of stack, and end of stack hence CreateException function should be present
            Assert.Equal("Microsoft.ApplicationInsights.Extensibility.Implementation.ExceptionConverterTest.FailedFunction", expDetails.parsedStack[0].method);
            Assert.Equal("Microsoft.ApplicationInsights.Extensibility.Implementation.ExceptionConverterTest.CreateException", expDetails.parsedStack[expDetails.parsedStack.Count - 1].method);
        }

#if !NETCOREAPP1_1

        [TestMethod]
        public void CheckThatFileNameAndLineAreCorrectIfAvailable()
        {
            var exp = this.CreateException(1);

            StackTrace st = new StackTrace(exp, true);
            var frame = st.GetFrame(0);
            var line = frame.GetFileLineNumber();
            var fileName = frame.GetFileName();

            ExceptionDetails expDetails = ExceptionConverter.ConvertToExceptionDetails(exp, null);
            var stack = expDetails.parsedStack;

            if (line != 0)
            {
                Assert.Equal(line, stack[0].line);
                Assert.Equal(fileName, stack[0].fileName);
            }
            else
            {
                Assert.Equal(0, stack[0].line);
                Assert.Null(stack[0].fileName);
            }
        }
#endif

#if !NETCOREAPP1_1

        [TestMethod]
        public void CheckThatAssemblyNameHasCorrectValue()
        {
            var exp = this.CreateException(2);

            ExceptionDetails expDetails = ExceptionConverter.ConvertToExceptionDetails(exp, null);

            string assemblyFullName = Assembly.GetExecutingAssembly().FullName;

            foreach (var stackFrame in expDetails.parsedStack)
            {
                Assert.Equal(assemblyFullName.ToLowerInvariant(), stackFrame.assembly.ToLowerInvariant());
            }
        }
#endif

        [TestMethod]
        public void CheckLevelCorrespondsToFrameForLongStack()
        {
            const int NumberOfStackFrames = 100;

            var exp = this.CreateException(NumberOfStackFrames - 1);

            ExceptionDetails expDetails = ExceptionConverter.ConvertToExceptionDetails(exp, null);
            var stack = expDetails.parsedStack;

            // Checking levels for first few and last few.
            for (int i = 0; i < 10; ++i)
            {
                Assert.Equal(i, stack[i].level);
            }

            for (int j = NumberOfStackFrames - 1, i = 0; j > NumberOfStackFrames - 10; --j, i++)
            {
                Assert.Equal(j, stack[stack.Count - 1 - i].level);
            }
        }

        [TestMethod]
        public void SizeOfParsedStackFrameIsLessThanMaxValue()
        {
            var exp = this.CreateException(300);

            ExceptionDetails expDetails = ExceptionConverter.ConvertToExceptionDetails(exp, null);
            int parsedStackLength = 0;

            var stack = expDetails.parsedStack;
            for (int i = 0; i < stack.Count; i++)
            {
                parsedStackLength += (stack[i].method == null ? 0 : stack[i].method.Length)
                                     + (stack[i].assembly == null ? 0 : stack[i].assembly.Length)
                                     + (stack[i].fileName == null ? 0 : stack[i].fileName.Length);
            }
            Assert.True(parsedStackLength <= ExceptionConverter.MaxParsedStackLength);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private Exception CreateException(int numberOfStackpoints)
        {
            Exception exception = null;

            try
            {
                this.FailedFunction(numberOfStackpoints);
            }
            catch (Exception exp)
            {
                exception = exp;
            }

            return exception;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private void FailedFunction(int numberOfStackpoints)
        {
            if (numberOfStackpoints > 1)
            {
                this.FailedFunction(--numberOfStackpoints);
            }

            throw new AggregateException("exception message");
        }
    }
}