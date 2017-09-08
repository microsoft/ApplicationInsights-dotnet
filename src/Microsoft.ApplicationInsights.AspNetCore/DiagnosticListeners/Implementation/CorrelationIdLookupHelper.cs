namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility;

#if NET451 || NET46
    using System.IO;
    using System.Net;
#else
    using System.Net.Http;
#endif

    /// <summary>
    /// A store for instrumentation App Ids. This makes sure we don't query the public endpoint to find an app Id for the same instrumentation key more than once.
    /// </summary>
    internal class CorrelationIdLookupHelper : ICorrelationIdLookupHelper
    {
        /// <summary>
        /// Max number of app ids to cache.
        /// </summary>
        private const int MAXSIZE = 100;

        // For now we have decided to go with not waiting to retrieve the app Id, instead we just cache it on retrieval.
        // This means the initial few attempts to get correlation id might fail and the initial telemetry sent might be missing such data.
        // However, once it is in the cache - subsequent telemetry should contain this data. 
        private const int GetAppIdTimeout = 0; // milliseconds

        internal const string CorrelationIdFormat = "cid-v1:{0}";

        private const string AppIdQueryApiRelativeUriFormat = "api/profiles/{0}/appId";

        private Func<TelemetryConfiguration> configurationProvider;

        private Uri endpointAddress;
        // Get the base URI, so that we can append the known relative segments to it.
        private Uri EndpointAddress
        {
            get
            {
                if (endpointAddress == null)
                {
                    if (!string.IsNullOrEmpty(configurationProvider()?.TelemetryChannel?.EndpointAddress))
                    {
                        Uri endpointUri = new Uri(configurationProvider().TelemetryChannel.EndpointAddress);
                        endpointAddress = new Uri(endpointUri.AbsoluteUri.Substring(0, endpointUri.AbsoluteUri.Length - endpointUri.LocalPath.Length));
                    }
                }
                return endpointAddress;
            }
        }

        private ConcurrentDictionary<string, string> knownCorrelationIds = new ConcurrentDictionary<string, string>();

        // Dedup dictionary to hold task status. Use iKey as Key, use task id as Value.
        private ConcurrentDictionary<string, int> iKeyTaskIdMapping = new ConcurrentDictionary<string, int>();

        private Func<string, Task<string>> provideAppId;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationIdLookupHelper" /> class mostly to be used by the test classes to provide an override for fetching appId logic.
        /// </summary>
        /// <param name="appIdProviderMethod">The delegate to be called to fetch the appId.</param>
        public CorrelationIdLookupHelper(Func<string, Task<string>> appIdProviderMethod)
        {
            if (appIdProviderMethod == null)
            {
                throw new ArgumentNullException(nameof(appIdProviderMethod));
            }

            this.provideAppId = appIdProviderMethod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationIdLookupHelper" /> class.
        /// </summary>
        /// <param name="configurationProvider">A delegate that provides telemetry configuration, which is to be used to fetch endpoint address.</param>
        public CorrelationIdLookupHelper(Func<TelemetryConfiguration> configurationProvider)
        {
            this.configurationProvider = configurationProvider;
            this.provideAppId = this.FetchAppIdFromService;
        }

        /// <summary>
        /// Retrieves the correlation id corresponding to a given instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key string.</param>
        /// <param name="correlationId">AppId corresponding to the provided instrumentation key.</param>
        /// <returns>true if correlationId was successfully retrieved; false otherwise.</returns>
        public bool TryGetXComponentCorrelationId(string instrumentationKey, out string correlationId)
        {
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                // This method cannot throw - it's a Try... method. We cannot proceed any further.
                correlationId = string.Empty;
                return false;
            }

            var found = this.knownCorrelationIds.TryGetValue(instrumentationKey, out correlationId);

            if (found)
            {
                return true;
            }
            else
            {
                // Simplistic cleanup to guard against this becoming a memory hog.
                if (this.knownCorrelationIds.Keys.Count >= MAXSIZE)
                {
                    this.knownCorrelationIds.Clear();
                }

                try
                {
                    // We wait for <getAppIdTimeout> seconds (which is 0 at this point) to retrieve the appId. If retrieved during that time, we return success setting the correlation id.
                    // If we are still waiting on the result beyond the timeout - for this particular call we return the failure but queue a task continuation for it to be cached for next time.
                    string iKeyLowered = instrumentationKey.ToLowerInvariant();
                    int taskId;
                    if (!this.iKeyTaskIdMapping.TryGetValue(iKeyLowered, out taskId))
                    {
                        Task<string> getAppIdTask = this.provideAppId(iKeyLowered);
                        if (getAppIdTask.Wait(GetAppIdTimeout))
                        {
                            if (string.IsNullOrEmpty(getAppIdTask.Result))
                            {
                                correlationId = string.Empty;
                            }
                            else
                            {
                                correlationId = this.GenerateCorrelationIdAndAddToDictionary(instrumentationKey, getAppIdTask.Result);
                            }
                            return true;
                        }
                        else
                        {
                            this.iKeyTaskIdMapping.TryAdd(iKeyLowered, getAppIdTask.Id);
                            getAppIdTask.ContinueWith((appId) =>
                            {
                                try
                                {
                                    this.GenerateCorrelationIdAndAddToDictionary(instrumentationKey, appId.Result);
                                }
                                catch (Exception ex)
                                {
                                    AspNetCoreEventSource.Instance.LogFetchAppIdFailed(ExceptionUtilities.GetExceptionDetailString(ex));
                                }
                                finally
                                {
                                    int currentTaskId;
                                    iKeyTaskIdMapping.TryRemove(iKeyLowered, out currentTaskId);
                                }
                            });
                            return false;
                        }
                    }
                    else
                    {
                        // A existing task is running; Report false for not getting the appId yet.
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    AspNetCoreEventSource.Instance.LogFetchAppIdFailed(ExceptionUtilities.GetExceptionDetailString(ex));
                    correlationId = string.Empty;
                    return false;
                }
            }
        }

        private string GenerateCorrelationIdAndAddToDictionary(string ikey, string appId)
        {
            string correlationId = string.Format(CultureInfo.InvariantCulture, CorrelationIdFormat, appId);
            this.knownCorrelationIds.TryAdd(ikey, correlationId);
            return correlationId;
        }

        /// <summary>
        /// Retrieves the app id given the instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key for which app id is to be retrieved.</param>
        /// <returns>App id.</returns>
        private async Task<string> FetchAppIdFromService(string instrumentationKey)
        {
            try
            {
                Uri appIdEndpoint = this.GetAppIdEndPointUri(instrumentationKey);
                if (appIdEndpoint == null)
                {
                    return null;
                }

                string result = null;
#if NET451 || NET46
                WebRequest request = WebRequest.Create(appIdEndpoint);
                request.Method = "GET";
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            result = await reader.ReadToEndAsync();
                        }
                    }
                }
#else
                using (HttpClient client = new HttpClient())
                {
                    var resultMessage = await client.GetAsync(appIdEndpoint).ConfigureAwait(false);
                    if (resultMessage.IsSuccessStatusCode)
                        result = await resultMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
#endif
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Strips off any relative path at the end of the base URI and then appends the known relative path to get the app id uri.
        /// </summary>
        /// <param name="instrumentationKey">AI resource's instrumentation key.</param>
        /// <returns>Computed Uri.</returns>
        private Uri GetAppIdEndPointUri(string instrumentationKey)
        {
            if (this.EndpointAddress != null)
            {
                return new Uri(this.EndpointAddress, string.Format(CultureInfo.InvariantCulture, AppIdQueryApiRelativeUriFormat, instrumentationKey));
            }
            return null;
        }
    }
}
