namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    
    using Assert = Xunit.Assert;
#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public class PartialSuccessTransmissionPolicyTest
    {
        [TestMethod]
        public void IfItemIsRejectedOnlyThisItemIsUploadedBack()
        {
            IList<Transmission> enqueuedTransmissions = new List<Transmission>();
            var transmitter = new StubTransmitter
            {
                OnEnqueue = t => { enqueuedTransmissions.Add(t); }
            };

            var policy = new PartialSuccessTransmissionPolicy();
            policy.Initialize(transmitter);

            var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
            Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

            string response = this.GetBackendResponse(
                itemsReceived: 2, 
                itemsAccepted: 1, 
                errorCodes: new[] { "429" });

            var wrapper = new HttpWebResponseWrapper
            {
                StatusCode = 206,
                Content = response
            };

            transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, wrapper));

            Assert.Equal(1, enqueuedTransmissions.Count);
        }

        [TestMethod]
        public void IfItemsAreRejectedTheyAreUploadedBackGroupedByStatusCode()
        {
            IList<Transmission> enqueuedTransmissions = new List<Transmission>();
            var transmitter = new StubTransmitter
            {
                OnEnqueue = t => { enqueuedTransmissions.Add(t); }
            };

            var policy = new PartialSuccessTransmissionPolicy();
            policy.Initialize(transmitter);

            var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
            Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

            string response = this.GetBackendResponse(
                itemsReceived: 2,
                itemsAccepted: 0,
                errorCodes: new[] { "408", "408" });

            var wrapper = new HttpWebResponseWrapper
            {
                StatusCode = 206,
                Content = response
            };
            transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, wrapper));

            Assert.Equal(1, enqueuedTransmissions.Count);
        }

        [TestMethod]
        public void IfNumberOfRecievedItemsEqualsToNumberOfAcceptedErrorsListIsIgnored()
        {
            var transmitter = new StubTransmitter();

            var policy = new PartialSuccessTransmissionPolicy();
            policy.Initialize(transmitter);

            var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
            Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

            string response = this.GetBackendResponse(
                itemsReceived: 2,
                itemsAccepted: 2,
                errorCodes: new[] { "408", "408" });

            var wrapper = new HttpWebResponseWrapper
            {
                StatusCode = 206,
                Content = response
            };

            transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, wrapper));

            Assert.Equal(0, policy.ConsecutiveErrors);
        }

        [TestMethod]
        public void NewTransmissionCreatedByIndexesFromResponse()
        {
            IList<Transmission> enqueuedTransmissions = new List<Transmission>();
            var transmitter = new StubTransmitter
            {
                OnEnqueue = t => { enqueuedTransmissions.Add(t); }
            };

            var policy = new PartialSuccessTransmissionPolicy();
            policy.Initialize(transmitter);

            var items = new List<ITelemetry>
                {
                    new EventTelemetry("1"),
                    new EventTelemetry("2"),
                    new EventTelemetry("3"),
                };
            Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

            string response = this.GetBackendResponse(
                itemsReceived: 3,
                itemsAccepted: 1,
                errorCodes: new[] { "439", "439" });

            var wrapper = new HttpWebResponseWrapper
            {
                StatusCode = 206,
                Content = response
            };

            transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, wrapper));

            string[] newItems = JsonSerializer
                .Deserialize(enqueuedTransmissions[0].Content)
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(2, newItems.Length);
            Assert.True(newItems[0].Contains("\"name\":\"1\""));
            Assert.True(newItems[1].Contains("\"name\":\"2\""));
        }

        [TestMethod]
        public void IfMultipleItemsAreRejectedNumberOfErrorsIsIncreasedByOne()
        {
            // Number of errors determine backoff timeout. 
            // When we get several bad items in one batch we want to increase errors by 1 only since it is one attempt to access Breeze

            var transmitter = new StubTransmitter();

            var policy = new PartialSuccessTransmissionPolicy();
            policy.Initialize(transmitter);

            var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
            Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

            string response = this.GetBackendResponse(
                itemsReceived: 100,
                itemsAccepted: 95,
                errorCodes: new[] { "500", "500", "503", "503", "429" });

            var wrapper = new HttpWebResponseWrapper
            {
                StatusCode = 206,
                Content = response
            };

            transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, wrapper));

            Assert.Equal(1, policy.ConsecutiveErrors);
        }

        [TestMethod]
        public void IfResponseIsBadJsonWeDoNotIncreaseErrorCount()
        {
            IList<Transmission> enqueuedTransmissions = new List<Transmission>();
            var transmitter = new StubTransmitter
            {
                OnEnqueue = t => { enqueuedTransmissions.Add(t); }
            };

            var policy = new PartialSuccessTransmissionPolicy();
            policy.Initialize(transmitter);

            var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
            Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

            string response = "[,]";

            var wrapper = new HttpWebResponseWrapper
            {
                StatusCode = 206,
                Content = response
            };

            transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, wrapper));

            Assert.Equal(0, policy.ConsecutiveErrors);
            Assert.Equal(0, enqueuedTransmissions.Count);
        }

        [TestMethod]
        public void IfIndexMoreThanNumberOfItemsInTransmissionIgnoreError()
        {
            IList<Transmission> enqueuedTransmissions = new List<Transmission>();
            var transmitter = new StubTransmitter
            {
                OnEnqueue = t => { enqueuedTransmissions.Add(t); }
            };

            var policy = new PartialSuccessTransmissionPolicy();
            policy.Initialize(transmitter);

            var items = new List<ITelemetry> { new EventTelemetry() };
            Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

            // Index is 0-based
            string response = this.GetBackendResponse(
                itemsReceived: 2,
                itemsAccepted: 1,
                errorCodes: new[] { "408" },
                indexStartWith: 1);

            var wrapper = new HttpWebResponseWrapper
            {
                StatusCode = 206,
                Content = response
            };

            transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, wrapper));

            Assert.Equal(0, policy.ConsecutiveErrors);
            Assert.Equal(0, enqueuedTransmissions.Count);
        }

        [TestMethod]
        public void DoesNotSendTransmissionForUnsupportedCodes()
        {
            IList<Transmission> enqueuedTransmissions = new List<Transmission>();
            var transmitter = new StubTransmitter
            {
                OnEnqueue = t => { enqueuedTransmissions.Add(t); }
            };

            var policy = new PartialSuccessTransmissionPolicy();
            policy.Initialize(transmitter);

            var items = new List<ITelemetry> { new EventTelemetry(), new EventTelemetry() };
            Transmission transmission = new Transmission(new Uri("http://uri"), items, "type", "encoding");

            string response = this.GetBackendResponse(
                itemsReceived: 50,
                itemsAccepted: 45,
                errorCodes: new[] { "400", "402", "502", "409", "417" });

            var wrapper = new HttpWebResponseWrapper
            {
                StatusCode = 206,
                Content = response
            };

            transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, null, wrapper));

            Assert.Equal(0, policy.ConsecutiveErrors);
            Assert.Equal(0, enqueuedTransmissions.Count);
        }

        private string GetBackendResponse(int itemsReceived, int itemsAccepted, string[] errorCodes, int indexStartWith = 0)
        {
            string singleItem = "{{" +
                                "\"index\": {0}," +
                                "\"statusCode\": {1}," +
                                "\"message\": \"Explanation\"" +
                                "}}";

            string errorList = string.Empty;
            for (int i=0; i<errorCodes.Length; ++i)
            {
                string errorCode = errorCodes[i];
                if (!string.IsNullOrEmpty(errorList))
                {
                    errorList += ",";
                }

                errorList += string.Format(CultureInfo.InvariantCulture, singleItem, indexStartWith + i, errorCode);
            }

            return 
               "{" +
                "\"itemsReceived\": " + itemsReceived + "," +
                "\"itemsAccepted\": " + itemsAccepted + "," +
                "\"errors\": [" + errorList+ "]" +
               "}";
        }
    }
}
