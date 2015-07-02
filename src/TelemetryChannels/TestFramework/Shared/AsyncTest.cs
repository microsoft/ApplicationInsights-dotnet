namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.TestFramework;

    /// <summary>
    /// A base class that helps to make unobserved task exceptions more noticeable.
    /// </summary>
    public class AsyncTest : IDisposable
    {
        private readonly TaskExceptionObserver taskExceptionObserver = new TaskExceptionObserver();

        /// <summary>
        /// Helps to run asynchronous tests on .NET 3.5.
        /// </summary>
        public static void Run(Func<Task> asyncMethod)
        {
            asyncMethod().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Disposes all resources.
        /// </summary>
        /// <param name="disposing">A disposing boolean as the dispose pattern requires.</param> 
        protected virtual void Dispose(bool disposing)
        {
            this.taskExceptionObserver.Dispose();
        }
    }
}
