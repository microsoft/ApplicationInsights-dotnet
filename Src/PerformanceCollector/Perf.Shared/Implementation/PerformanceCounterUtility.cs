namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Utility functionality for performance counter collection.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "This class has different code for Net45/NetCore")]
    internal static class PerformanceCounterUtility
    {
        private const string Win32ProcessInstancePlaceholder = @"APP_WIN32_PROC";
        private const string ClrProcessInstancePlaceholder = @"APP_CLR_PROC";
        private const string W3SvcProcessInstancePlaceholder = @"APP_W3SVC_PROC";

        private const string Win32ProcessCategoryName = "Process";
        private const string ClrProcessCategoryName = ".NET CLR Memory";
        private const string Win32ProcessCounterName = "ID Process";
        private const string ClrProcessCounterName = "Process ID";        
#if NETSTANDARD2_0
        private const string StandardSdkVersionPrefix = "pccore:";
#else
        private const string StandardSdkVersionPrefix = "pc:";
#endif
        private const string AzureWebAppSdkVersionPrefix = "azwapc:";
        private const string AzureWebAppCoreSdkVersionPrefix = "azwapccore:";

        private const string WebSiteEnvironmentVariable = "WEBSITE_SITE_NAME";
        private const string ProcessorsCountEnvironmentVariable = "NUMBER_OF_PROCESSORS";

        private static readonly ConcurrentDictionary<string, string> PlaceholderCache = new ConcurrentDictionary<string, string>();

        private static readonly Regex InstancePlaceholderRegex = new Regex(
            @"^\?\?(?<placeholder>[a-zA-Z0-9_]+)\?\?$",
            RegexOptions.Compiled);

        private static readonly Regex PerformanceCounterRegex =
            new Regex(
                @"^\\(?<categoryName>[^(]+)(\((?<instanceName>[^)]+)\)){0,1}\\(?<counterName>[\s\S]+)$",
                RegexOptions.Compiled);

        private static bool? isAzureWebApp = null;

#if !NETSTANDARD1_6
        /// <summary>
        /// Formats a counter into a readable string.
        /// </summary>
        public static string FormatPerformanceCounter(PerformanceCounter pc)
        {
            return FormatPerformanceCounter(pc.CategoryName, pc.CounterName, pc.InstanceName);
        }
#endif

        /// <summary>
        /// Formats a counter into a readable string.
        /// </summary>
        /// <param name="pc">Performance counter structure.</param>
        public static string FormatPerformanceCounter(PerformanceCounterStructure pc)
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
        /// Gets the processor count from the appropriate environment variable depending on whether the app is a WebApp or not.
        /// </summary>
        /// <param name="isWebApp">Indicates whether the application is a WebApp or not.</param>
        /// <returns>The number of processors in the system or null if failed to determine.</returns>
        public static int? GetProcessorCount(bool isWebApp)
        {
            int count;

            if (!isWebApp)
            {
                count = Environment.ProcessorCount;
            }
            else
            {
                string countString;
                try
                {
                    countString = Environment.GetEnvironmentVariable(ProcessorsCountEnvironmentVariable);
                }
                catch (Exception ex)
                {
                    PerformanceCollectorEventSource.Log.ProcessorsCountIncorrectValueError(ex.ToString());
                    return null;
                }

                if (!int.TryParse(countString, out count))
                {
                    PerformanceCollectorEventSource.Log.ProcessorsCountIncorrectValueError(countString);
                    return null;
                }
            }

            if (count < 1 || count > 1000)
            {
                PerformanceCollectorEventSource.Log.ProcessorsCountIncorrectValueError(count.ToString(CultureInfo.InvariantCulture));
                return null;
            }

            return count;
        }

        /// <summary>
        /// Differentiates the SDK version prefix for azure web applications with standard applications.
        /// </summary>
        /// <returns>Returns the SDK version prefix based on the platform.</returns>
        public static string SDKVersionPrefix()
        {
            if (IsWebAppRunningInAzure())
            {
#if NETSTANDARD1_6 || NETSTANDARD2_0
                return AzureWebAppCoreSdkVersionPrefix;
#else
                return AzureWebAppSdkVersionPrefix;
#endif                                
            }
            else
            {
                return StandardSdkVersionPrefix;
            }
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
        /// Validates the counter by parsing.
        /// </summary>
        /// <param name="perfCounterName">Performance counter name to validate.</param>
        /// <param name="win32Instances">Windows 32 instances.</param>
        /// <param name="clrInstances">CLR instances.</param>
        /// <param name="usesInstanceNamePlaceholder">Boolean to check if it is using an instance name place holder.</param>
        /// <param name="error">Error message.</param>
        /// <returns>Performance counter.</returns>
        public static PerformanceCounterStructure CreateAndValidateCounter(
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
        public static PerformanceCounterStructure ParsePerformanceCounter(
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

            return new PerformanceCounterStructure()
            {
                CategoryName = match.Groups["categoryName"].Value,
                InstanceName =
                    ExpandInstanceName(
                        match.Groups["instanceName"].Value,
                        win32Instances,
                        clrInstances,
                        out usesInstanceNamePlaceholder),
                CounterName = match.Groups["counterName"].Value,
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
#if NETSTANDARD1_6 || NETSTANDARD2_0
            string name = new AssemblyName(Assembly.GetEntryAssembly().FullName).Name;
#else
            string name = AppDomain.CurrentDomain.FriendlyName;
#endif

            return GetInstanceFromApplicationDomain(name);
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

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "This method has different code for Net45/NetCore")]
        internal static string GetInstanceForWin32Process(IEnumerable<string> win32Instances)
        {
#if NETSTANDARD1_6
            return string.Empty;
#else
            return FindProcessInstance(
                Process.GetCurrentProcess().Id,
                win32Instances,
                Win32ProcessCategoryName,
                Win32ProcessCounterName);
#endif
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "This method has different code for Net45/NetCore")]
        internal static string GetInstanceForClrProcess(IEnumerable<string> clrInstances)
        {
#if NETSTANDARD1_6
            return string.Empty;
#else
            return FindProcessInstance(
                Process.GetCurrentProcess().Id,
                clrInstances,
                ClrProcessCategoryName,
                ClrProcessCounterName);
#endif
        }

#if !NETSTANDARD1_6
        internal static IList<string> GetWin32ProcessInstances()
        {
            return GetInstances(Win32ProcessCategoryName);
        }

        internal static IList<string> GetClrProcessInstances()
        {
            return GetInstances(ClrProcessCategoryName);
        }

#endif

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

#if !NETSTANDARD1_6
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
#if NETSTANDARD2_0
                return Array.Empty<string>();
#else
                return new string[] { };
#endif
            }
        }
#endif
            }
}