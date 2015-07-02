namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{    
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates telemetry location information.
    /// </summary>
    public sealed class LocationContext : IJsonSerializable
    {
        private readonly IDictionary<string, string> tags;

        internal LocationContext(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the location IP.
        /// </summary>
        public string Ip
        {
            get 
            { 
                return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.LocationIp); 
            }

            set 
            {
                if (value != null && this.IsIpV4(value))
                {
                    this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.LocationIp, value);
                }
            }
        }

        void IJsonSerializable.Serialize(IJsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteProperty("ip", this.Ip);
            writer.WriteEndObject();
        }

        private bool IsIpV4(string ip)
        {
            if ((ip.Length > 15) || (ip.Length < 7))
            {
                return false;
            }

            if (Enumerable.Any<char>(
                    Enumerable.Cast<char>((IEnumerable)ip), 
                    delegate(char c)
                    {
                        if ((c >= '0') && (c <= '9'))
                        {
                            return false;
                        }

                        return c != '.';
                    }))
            {
                return false;
            }

            string[] strArray = ip.Split(new char[] { '.' });
            if (strArray.Length != 4)
            {
                return false;
            }

            foreach (string str in strArray)
            {
                byte num;
                if (!byte.TryParse(str, out num))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
