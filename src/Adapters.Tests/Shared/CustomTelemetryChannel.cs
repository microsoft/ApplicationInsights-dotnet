//-----------------------------------------------------------------------------------
// <copyright file='CustomTelemetryChannel.cs' company='Microsoft Corporation'>
//     Copyright (c) Microsoft Corporation. All Rights Reserved.
//     Information Contained Herein is Proprietary and Confidential.
// </copyright>
//-----------------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    internal class CustomTelemetryChannel : ITelemetryChannel
    {
        private EventWaitHandle waitHandle;

        public CustomTelemetryChannel()
        {
            this.waitHandle = new AutoResetEvent(false);
            this.SentItems = new ITelemetry[0];
        }

        public bool? DeveloperMode { get; set; }

        public string EndpointAddress { get; set; }

        public ITelemetry[] SentItems { get; private set; }

        public void Send(ITelemetry item)
        {
            lock (this)
            {
                ITelemetry[] current = this.SentItems;
                List<ITelemetry> temp = new List<ITelemetry>(current);
                temp.Add(item);
                this.SentItems = temp.ToArray();
                this.waitHandle.Set();
            }
        }

        public Task WaitOneItemAsync(TimeSpan timeout)
        {
            return Task.Factory.StartNew(() => this.waitHandle.WaitOne(timeout));
        }

        public void Flush()
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
        }

        public CustomTelemetryChannel Reset()
        {
            lock (this)
            {
                this.SentItems = new ITelemetry[0];
            }

            return this;
        }
    }
}
