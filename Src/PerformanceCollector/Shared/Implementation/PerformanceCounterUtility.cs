namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Utility functionality for performance counter collection.
    /// </summary>
    internal static class PerformanceCounterUtility
    {
        private const string Win32ProcessInstancePlaceholder = @"APP_WIN32_PROC";
        private const string ClrProcessInstancePlaceholder = @"APP_CLR_PROC";
        private const string W3SvcProcessInstancePlaceholder = @"APP_W3SVC_PROC";
        
        private const string Win32ProcessCategoryName = "Process";
        private const string ClrProcessCategoryName = ".NET CLR Memory";
        private const string Win32ProcessCounterName = "ID Process";
        private const string ClrProcessCounterName = "Process ID";

        private const string StandardSdkVersionPrefix = "pc:";
        private const string AzureWebAppSdkVersionPrefix = "azwapc:";

        private const string WebSiteEnvironmentVariable = "WEBSITE_SITE_NAME";

        private static readonly Dictionary<string, string> PlaceholderCache = new Dictionary<string, string>();

        private static readonly Regex InstancePlaceholderRegex = new Regex(
            @"^\?\?(?<placeholder>[a-zA-Z0-9_]+)\?\?$",
            RegexOptions.Compiled);

        private static readonly Regex PerformanceCounterRegex =
            new Regex(
                @"^\\(?<categoryName>[^(]+)(\((?<instanceName>[^)]+)\)){0,1}\\(?<counterName>[\s\S]+)$",
                RegexOptions.Compiled);

        private static bool? isAzureWebApp = null;

        /// <summary>
        /// Formats a counter into a readable string.
        /// </summary>
        public static string FormatPerformanceCounter(PerformanceCounter pc)
        {
            return FormatPerformanceCounter(pc.CategoryName, pc.CounterName, pc.InstanceName);
        }

        /// <summary>
        /// Searches for the environment variable specific to Azure web applications and confirms if the current application is a web application or not.
        /// </summary>
        /// <returns>Boolean, which is true if the current application is an Azure web application.</returns>
        public static bool IsWebAppRunningInAzure()
        {
            if (!isAzureWebApp.HasValue)
            {
                try
                {
                    isAzureWebApp = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(WebSiteEnvironmentVariable));
                } 
                catch (Exception ex)
                {
                    PerformanceCollectorEventSource.Log.AccessingEnvironmentVariableFailedWarning(WebSiteEnvironmentVariable, ex.ToString());
                    return false;
                }
            }

            return (bool)isAzureWebApp;
        }

        /// <summary>
        /// Differentiates the SDK version prefix for azure web applications with standard applications.
        /// </summary>
        /// <returns>Returns the SDK version prefix based on the platform.</returns>
        public static string SDKVersionPrefix()
        {
            return IsWebAppRunningInAzure() ? AzureWebAppSdkVersionPrefix : StandardSdkVersionPrefix;
        }

        /// <summary>
        /// Formats a counter into a readable string.
        /// </summary>
        public static string FormatPerformanceCounter(string categoryName, string counterName, string instanceName)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
            {
                return string.Format(CultureInfo.InvariantCulture, @"\{0}\{1}", categoryName, counterName);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                @"\{0}({2})\{1}",
                categoryName,
                counterName,
                instanceName);
        }

        /// <summary>
        /// Parses a performance counter canonical string into a PerformanceCounter object.
        /// </summary>
        /// <remarks>This method also performs placeholder expansion.</remarks>
        public static PerformanceCounter ParsePerformanceCounter(
            string performanceCounter,
            IEnumerable<string> win32Instances,
            IEnumerable<string> clrInstances)
        {
            bool usesInstanceNamePlaceholder;

            return ParsePerformanceCounter(
                performanceCounter,
                win32Instances,
                clrInstances,
                out usesInstanceNamePlaceholder);
        }

        /// <summary>
        /// Validates the counter by parsing.
        /// </summary>
        /// <param name="perfCounterName">Performance counter name to validate.</param>
        /// <param name="win32Instances">Windows 32 instances.</param>
        /// <param name="clrInstances">CLR instances.</param>
        /// <param name="usesInstanceNamePlaceholder">Boolean to check if it is using an instance name place holder.</param>
        /// <param name="error">Error message.</param>
        /// <returns>Performance counter.</returns>
        public static PerformanceCounter CreateAndValidateCounter(
            string perfCounterName,
            IEnumerable<string> win32Instances,
            IEnumerable<string> clrInstances,
            out bool usesInstanceNamePlaceholder,
            out string error)
        {
            error = null;

            try
            {
                return PerformanceCounterUtility.ParsePerformanceCounter(
                    perfCounterName,
                    win32Instances,
                    clrInstances,
                    out usesInstanceNamePlaceholder);
            }
            catch (Exception e)
            {
                usesInstanceNamePlaceholder = false;
                PerformanceCollectorEventSource.Log.CounterParsingFailedEvent(e.Message, perfCounterName);
                error = e.Message;

                return null;
            }
        }

        /// <summary>
        /// Parses a performance counter canonical string into a PerformanceCounter object.
        /// </summary>
        /// <remarks>This method also performs placeholder expansion.</remarks>
        public static PerformanceCounter ParsePerformanceCounter(
            string performanceCounter,
            IEnumerable<string> win32Instances,
            IEnumerable<string> clrInstances,
            out bool usesInstanceNamePlaceholder)
        {
            var match = PerformanceCounterRegex.Match(performanceCounter);

            if (!match.Success)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"Invalid performance counter name format: {0}. Expected formats are \category(instance)\counter or \category\counter",
                        performanceCounter),
                    nameof(performanceCounter));
            }

            return new PerformanceCounter()
                       {
                           CategoryName = match.Groups["categoryName"].Value,
                           InstanceName =
                               ExpandInstanceName(
                                   match.Groups["instanceName"].Value,
                                   win32Instances,
                                   clrInstances,
                                   out usesInstanceNamePlaceholder),
                           CounterName = match.Groups["counterName"].Value
                       };
        }

        /// <summary>
        /// Invalidates placeholder cache.
        /// </summary>
        public static void InvalidatePlaceholderCache()
        {
            PlaceholderCache.Clear();
        }

        /// <summary>
        /// Matches an instance name against the placeholder regex.
        /// </summary>
        /// <param name="instanceName">Instance name to match.</param>
        /// <returns>Regex match.</returns>
        public static Match MatchInstancePlaceholder(string instanceName)
        {
            var match = InstancePlaceholderRegex.Match(instanceName);

            if (!match.Success || !match.Groups["placeholder"].Success)
            {
                return null;
            }
            
            return match;
        }

        internal static string GetInstanceForCurrentW3SvcWorker()
        {
            return GetInstanceFromApplicationDomain(AppDomain.CurrentDomain.FriendlyName);
        }

        internal static string GetInstanceFromApplicationDomain(string domainFriendlyName)
        {
            const string Separator = "-";

            string[] segments = domainFriendlyName.Split(Separator.ToCharArray());

            var nameWithoutTrailingData = string.Join(
                Separator,
                segments.Take(segments.Length > 2 ? segments.Length - 2 : segments.Length));

            return nameWithoutTrailingData.Replace('/', '_');
        }

        internal static string GetInstanceForWin32Process(IEnumerable<string> win32Instances)
        {
            return FindProcessInstance(
                Process.GetCurrentProcess().Id,
                win32Instances,
                Win32ProcessCategoryName,
                Win32ProcessCounterName);
        }

        internal static string GetInstanceForClrProcess(IEnumerable<string> clrInstances)
        {
            return FindProcessInstance(
                Process.GetCurrentProcess().Id,
                clrInstances,
                ClrProcessCategoryName,
                ClrProcessCounterName);
        }

        internal static IList<string> GetWin32ProcessInstances()
        {
            return GetInstances(Win32ProcessCategoryName);
        }

        internal static IList<string> GetClrProcessInstances()
        {
            return GetInstances(ClrProcessCategoryName);
        }

        private static string ExpandInstanceName(
            string instanceName,
            IEnumerable<string> win32Instances,
            IEnumerable<string> clrInstances,
            out bool usesPlaceholder)
        {
            var match = MatchInstancePlaceholder(instanceName);
            if (match == null)
            {
                // not a placeholder, do not expand
                usesPlaceholder = false;
                return instanceName;
            }

            usesPlaceholder = true;

            if (IsWebAppRunningInAzure())
            {
                return instanceName;
            } 

            var placeholder = match.Groups["placeholder"].Value;

            // use a cached value if available
            string cachedResult;
            if (PlaceholderCache.TryGetValue(placeholder, out cachedResult))
            {
                return cachedResult;
            }

            // expand
            if (string.Equals(placeholder, Win32ProcessInstancePlaceholder, StringComparison.OrdinalIgnoreCase))
            {
                cachedResult = GetInstanceForWin32Process(win32Instances);
            }
            else if (string.Equals(placeholder, ClrProcessInstancePlaceholder, StringComparison.OrdinalIgnoreCase))
            {
                cachedResult = GetInstanceForClrProcess(clrInstances);
            }
            else if (string.Equals(placeholder, W3SvcProcessInstancePlaceholder, StringComparison.OrdinalIgnoreCase))
            {
                cachedResult = GetInstanceForCurrentW3SvcWorker();
            }
            else
            {
                // a non-supported placeholder, return as is
                return instanceName;
            }

            // add to cache
            PlaceholderCache[placeholder] = cachedResult;

            return cachedResult;
        }
        
        private static string FindProcessInstance(
            int pid,
            IEnumerable<string> instances,
            string categoryName,
            string counterName)
        {
            return instances.FirstOrDefault(
                i =>
                    {
                        try
                        {
                            return pid == (int)new PerformanceCounter(categoryName, counterName, i, true).RawValue;
                        }
                        catch (Exception)
                        {
                            // most likely the process has terminated since we got the process list
                            // that process is not us, we're still running
                            return false;
                        }
                    });
        }

        private static IList<string> GetInstances(string categoryName)
        {
            var cat = new PerformanceCounterCategory() { CategoryName = categoryName };

            try
            {
                return cat.GetInstanceNames();
            }
            catch (Exception)
            {
                // something went wrong and the category hasn't been found
                // we can't perform this operation
                return new string[] { };
            }
        }
    }
}