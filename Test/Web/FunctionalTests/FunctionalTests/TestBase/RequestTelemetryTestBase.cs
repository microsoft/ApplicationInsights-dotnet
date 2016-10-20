// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequestTelemetryTestBase.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// --------------------------------------------------------------------------------------------------------------------
namespace Functional.Helpers
{
    using System;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class RequestTelemetryTestBase : SingleWebHostTestBase
    {
        protected void TestWebApplicationHelper(string requestName, string requestUrl, string responseCode, bool success, TelemetryItem<RequestData> item, DateTimeOffset testStart, DateTimeOffset testFinish)
        {
            Assert.AreEqual(this.Config.IKey, item.iKey, "iKey is not the same as in config file");
            Assert.AreEqual(requestName, item.tags[new ContextTagKeys().OperationName]);
            Assert.AreEqual(requestUrl, item.data.baseData.url);
            Assert.AreEqual(responseCode, item.data.baseData.responseCode);
            Assert.AreEqual(success, item.data.baseData.success);

            double duration = Math.Floor(TimeSpan.Parse(item.data.baseData.duration).TotalMilliseconds);
            Assert.IsTrue(duration >= 0, "Duration is negative: " + duration);
            Assert.IsTrue((testFinish - testStart).TotalMilliseconds >= duration, "Duration is incorrect: " + duration);
        }
    }
}
