namespace Xunit
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Threading.Tasks;

    using Xunit.Sdk;

    internal class AssertEx
    {
        /// <summary>
        /// Verifies that a task does not throw any exceptions.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested.</param>
        public static async Task DoesNotThrowAsync(Func<Task> testCode)
        {
            AssertEx.DoesNotThrow(await AssertEx.ExceptionAsync(testCode));
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// </summary>
        /// <typeparam name="T">The type of the exception expected to be thrown.</typeparam>
        /// <param name="testCode">A delegate to the task to be tested.</param>
        /// <returns>The exception that was thrown, when successful.</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown.</exception>
        public static async Task<T> ThrowsAsync<T>(Func<Task> testCode)
            where T : Exception
        {
            return (T)AssertEx.Throws(typeof(T), await AssertEx.ExceptionAsync(testCode));
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// </summary>
        /// <typeparam name="T">The type of the exception expected to be thrown.</typeparam>
        /// <param name="testCode">A delegate to the task to be tested.</param>
        /// <returns>The exception that was thrown, when successful.</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown.</exception>
        public static async Task<T> ThrowsAnyAsync<T>(Func<Task> testCode)
            where T : Exception
        {
            return (T)AssertEx.ThrowsAny(typeof(T), await AssertEx.ExceptionAsync(testCode));
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// </summary>
        /// <param name="exceptionType">The type of the exception expected to be thrown.</param>
        /// <param name="testCode">A delegate to the task to be tested.</param>
        /// <returns>The exception that was thrown, when successful.</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown.</exception>
        public static async Task<Exception> ThrowsAsync(Type exceptionType, Func<Task> testCode)
        {
            return AssertEx.Throws(exceptionType, await AssertEx.ExceptionAsync(testCode));
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type), where the exception
        /// derives from <see cref="ArgumentException"/> and has the given parameter name.
        /// </summary>
        /// <param name="paramName">The parameter name that is expected to be in the exception.</param>
        /// <param name="testCode">A delegate to the task to be tested.</param>
        /// <returns>The exception that was thrown, when successful.</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown.</exception>
        public static async Task<T> ThrowsAsync<T>(string paramName, Func<Task> testCode)
            where T : ArgumentException
        {
            var ex = await AssertEx.ThrowsAsync<T>(testCode);
            Assert.Equal(paramName, ex.ParamName);
            return ex;
        }

        /// <summary>
        /// Verifies that a string starts with a given string, using the current culture.
        /// </summary>
        /// <param name="expectedStartString">The string expected to be at the start of the string.</param>
        /// <param name="actualString">The string to be inspected.</param>
        /// <exception cref="ContainsException">Thrown when the string does not start with the expected string.</exception>
        public static void StartsWith(string expectedStartString, string actualString)
        {
            StartsWith(expectedStartString, actualString, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Verifies that a string starts with a given string, using the given comparison type.
        /// </summary>
        /// <param name="expectedStartString">The string expected to be at the start of the string.</param>
        /// <param name="actualString">The string to be inspected.</param>
        /// <param name="comparisonType">The type of string comparison to perform.</param>
        /// <exception cref="ContainsException">Thrown when the string does not start with the expected string.</exception>
        public static void StartsWith(string expectedStartString, string actualString, StringComparison comparisonType)
        {
            if (expectedStartString == null || actualString == null || !actualString.StartsWith(expectedStartString, comparisonType))
            {
                throw new StartsWithException(expectedStartString, actualString);
            }
        }

        /// <summary>
        /// Verifies that a string ends with a given string, using the current culture.
        /// </summary>
        /// <param name="expectedEndString">The string expected to be at the end of the string.</param>
        /// <param name="actualString">The string to be inspected.</param>
        /// <exception cref="ContainsException">Thrown when the string does not end with the expected string.</exception>
        public static void EndsWith(string expectedEndString, string actualString)
        {
            EndsWith(expectedEndString, actualString, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Verifies that a string ends with a given string, using the given comparison type.
        /// </summary>
        /// <param name="expectedEndString">The string expected to be at the end of the string.</param>
        /// <param name="actualString">The string to be inspected.</param>
        /// <param name="comparisonType">The type of string comparison to perform.</param>
        /// <exception cref="ContainsException">Thrown when the string does not end with the expected string.</exception>
        public static void EndsWith(string expectedEndString, string actualString, StringComparison comparisonType)
        {
            if (expectedEndString == null || actualString == null || !actualString.EndsWith(expectedEndString, comparisonType))
            {
                throw new EndsWithException(expectedEndString, actualString);
            }
        }

        private static void DoesNotThrow(Exception exception)
        {
            if (exception != null)
            {
                throw new DoesNotThrowException(exception);
            }
        }

        private static Exception Throws(Type exceptionType, Exception exception)
        {
            if (exception == null)
            {
                throw new ThrowsException(exceptionType);
            }

            if (exceptionType != exception.GetType())
            {
                throw new ThrowsException(exceptionType, exception);
            }

            return exception;
        }

        private static Exception ThrowsAny(Type exceptionType, Exception exception)
        {
            if (exception == null)
            {
                throw new ThrowsException(exceptionType);
            }

            if (!exceptionType.GetTypeInfo().IsAssignableFrom(exception.GetType().GetTypeInfo()))
            {
                throw new ThrowsException(exceptionType, exception);
            }

            return exception;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The caught exception is resurfaced to the user.")]
        private static async Task<Exception> ExceptionAsync(Func<Task> testCode)
        {
            try
            {
                await testCode();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}