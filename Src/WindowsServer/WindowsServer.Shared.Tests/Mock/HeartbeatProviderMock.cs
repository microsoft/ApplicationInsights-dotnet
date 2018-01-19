using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
using System;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.WindowsServer.Mock
{
    class HeartbeatProviderMock : IHeartbeatPropertyManager
    {
        public bool enabled = true;
        public TimeSpan interval = TimeSpan.FromMinutes(15);
        public List<string> excludedProps = new List<string>();
        public Dictionary<string, string> hbeatProps = new Dictionary<string, string>();
        public Dictionary<string, bool> hbeatHealth = new Dictionary<string, bool>();

        public bool IsHeartbeatEnabled { get => enabled; set => enabled = value; }

        public TimeSpan HeartbeatInterval { get => interval; set => interval = value; }

        public IList<string> ExcludedHeartbeatProperties => excludedProps;

        public bool AddHeartbeatProperty(string propertyName, string propertyValue, bool isHealthy)
        {
            if (!hbeatProps.ContainsKey(propertyName))
            {
                hbeatProps.Add(propertyName, propertyValue);
                hbeatHealth.Add(propertyName, isHealthy);
                return true;
            }
            return false;
        }

        public bool SetHeartbeatProperty(string propertyName, string propertyValue = null, bool? isHealthy = null)
        {
            if (!string.IsNullOrEmpty(propertyName) && hbeatProps.ContainsKey(propertyName))
            {
                if (!string.IsNullOrEmpty(propertyValue))
                {
                    hbeatProps[propertyName] = propertyValue;
                }
                if (isHealthy.HasValue)
                {
                    hbeatHealth[propertyName] = isHealthy.GetValueOrDefault(false);
                }
                return true;
            }
            return false;
        }
    }
}
