namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Class to control default debug logger and disable it if logger was added to <see cref="ILoggerFactory"/> explicetely.
    /// </summary>
    internal class ApplicationInsightsLoggerCallbacks
    {
        /// <summary>
        /// Stores list of callbacks to execute when new ApplicationInsights logger is added.
        /// </summary>
        private readonly List<Action> callbacks;

        public ApplicationInsightsLoggerCallbacks()
        {
            callbacks = new List<Action>();
        }


        /// <summary>
        /// Registers a callback that executes when new ApplicationInsights logger is added.
        /// </summary>
        public void AddLoggerCallback(Action loggerAddedCallback)
        {
            lock (callbacks)
            {
                foreach (var callback in callbacks)
                {
                    callback();
                }

                callbacks.Add(loggerAddedCallback);
            }
        }
    }
}
