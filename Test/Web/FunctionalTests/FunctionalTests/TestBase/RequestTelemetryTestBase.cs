// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequestTelemetryTestBase.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// --------------------------------------------------------------------------------------------------------------------
namespace Functional.Helpers
{
    using System;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class RequestTelemetryTestBase : SingleWebHostTestBase
    {
        protected void TestWebApplicationHelper(string requestName, string requestUrl, string responseCode, bool success, TelemetryItem<RequestData> item, DateTimeOffset testStart, DateTimeOffset testFinish)
        {
            Assert.AreEqual(this.Config.IKey, item.IKey, "iKey is not the same as in config file");
            Assert.AreEqual(requestName, item.OperationContext.Name);
            Assert.AreEqual(requestUrl, item.Data.BaseData.Url);
            Assert.AreEqual(responseCode, item.Data.BaseData.ResponseCode);
            Assert.AreEqual(success, item.Data.BaseData.Success);

            double duration = Math.Floor(item.Data.BaseData.Duration.TotalMilliseconds);
            Assert.IsTrue(duration >= 0, "Duration is negative: " + duration);
            Assert.IsTrue((testFinish - testStart).TotalMilliseconds >= duration, "Duration is incorrect: " + duration);
        }
    }
}
