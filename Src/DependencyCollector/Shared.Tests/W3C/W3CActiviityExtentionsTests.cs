namespace Microsoft.ApplicationInsights.DependencyCollector.W3C
{
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.W3C;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class W3CActiviityExtentionsTests
    {
#pragma warning disable 612, 618

        private const string TraceId = "01010101010101010101010101010101";
        private const string ParenSpanId = "0202020202020202";

        [TestCleanup]
        public void Cleanup()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }

        [TestMethod]
        public void SetInvalidTraceParent()
        {
            var invalidTraceParents = new[]
            {
                "123", string.Empty, null, "00-00", "00-00-00", "00-00-00-", "-00-00-00", "00-00-00-00-00",
                "00-00-00- ", " -00-00-00", "---",  "00---", "00-00--", "00--00-", "00---00"
            };
            foreach (var traceparent in invalidTraceParents)
            {
                var a = new Activity("foo");
                a.SetTraceparent(traceparent);

                Assert.IsFalse(a.Tags.Any(t => t.Key == W3CConstants.ParentSpanIdTag), traceparent);
                Assert.IsNull(a.GetParentSpanId());
                Assert.IsNull(a.GetTracestate());

                Assert.AreEqual(W3CConstants.DefaultVersion, a.Tags.Single(t => t.Key == W3CConstants.VersionTag).Value, traceparent);
                Assert.AreEqual(W3CConstants.TraceFlagRecordedAndNotRequested, a.Tags.Single(t => t.Key == W3CConstants.SampledTag).Value, traceparent);

                Assert.IsTrue(a.IsW3CActivity(), traceparent);
                Assert.AreEqual(32, a.GetTraceId().Length, traceparent);
                Assert.AreEqual(16, a.GetSpanId().Length, traceparent);

                Assert.AreEqual($"{W3CConstants.DefaultVersion}-{a.GetTraceId()}-{a.GetSpanId()}-{W3CConstants.TraceFlagRecordedAndNotRequested}", a.GetTraceparent(), traceparent);
            }
        }

        [TestMethod]
        public void InvalidTraceIdAllTraceparentIsIgnored()
        {
            var invalidTraceIds = new[]
            {
                "123",
                "000102030405060708090a0b0c0d0f", // 30 chars
                "000102030405060708090a0b0c0d0f0", // 31 char
                "000102030405060708090a0b0c0d0f0g", // 32 char non-hex
                "000102030405060708090a0b0c0d0f0A", // 32 char upper case
                "000102030405060708090a0b0c0d0f000" // 33 chars
            };
            foreach (var traceId in invalidTraceIds)
            {
                var a = new Activity("foo");

                a.SetTraceparent($"00-{traceId}-{ParenSpanId}-00");

                Assert.IsFalse(a.Tags.Any(t => t.Key == W3CConstants.ParentSpanIdTag), traceId);
                Assert.IsNull(a.GetParentSpanId());
                Assert.IsNull(a.GetTracestate());

                Assert.AreEqual(W3CConstants.DefaultVersion, a.Tags.Single(t => t.Key == W3CConstants.VersionTag).Value, traceId);
                Assert.AreEqual(W3CConstants.TraceFlagRecordedAndNotRequested, a.Tags.Single(t => t.Key == W3CConstants.SampledTag).Value, traceId);

                Assert.IsTrue(a.IsW3CActivity(), traceId);
                Assert.AreEqual(32, a.GetTraceId().Length, traceId);
                Assert.AreEqual(16, a.GetSpanId().Length, traceId);

                Assert.AreEqual($"{W3CConstants.DefaultVersion}-{a.GetTraceId()}-{a.GetSpanId()}-{W3CConstants.TraceFlagRecordedAndNotRequested}", a.GetTraceparent(), traceId);
            }
        }

        [TestMethod]
        public void InvalidSapnIdAllTraceparentIsIgnored()
        {
            var invalidSpanIds = new[]
            {
                "123",
                "00010203040506", // 14 chars
                "000102030405060", // 15 char
                "000102030405060g", // 16 char non-hex
                "000102030405060A", // 16 char upper case
                "00010203040506070" // 15 chars
            };
            foreach (var parentSpanId in invalidSpanIds)
            {
                var a = new Activity("foo");

                a.SetTraceparent($"00-{TraceId}-{parentSpanId}-00");

                Assert.IsFalse(a.Tags.Any(t => t.Key == W3CConstants.ParentSpanIdTag), parentSpanId);
                Assert.IsNull(a.GetParentSpanId());
                Assert.IsNull(a.GetTracestate());

                Assert.AreEqual(W3CConstants.DefaultVersion, a.Tags.Single(t => t.Key == W3CConstants.VersionTag).Value, parentSpanId);
                Assert.AreEqual(W3CConstants.TraceFlagRecordedAndNotRequested, a.Tags.Single(t => t.Key == W3CConstants.SampledTag).Value, parentSpanId);

                Assert.IsTrue(a.IsW3CActivity(), parentSpanId);
                Assert.AreEqual(32, a.GetTraceId().Length, parentSpanId);
                Assert.AreEqual(16, a.GetSpanId().Length, parentSpanId);

                Assert.AreEqual($"{W3CConstants.DefaultVersion}-{a.GetTraceId()}-{a.GetSpanId()}-{W3CConstants.TraceFlagRecordedAndNotRequested}", a.GetTraceparent(), parentSpanId);
            }
        }

        [TestMethod]
        public void SetValidTraceParent()
        {
            var a = new Activity("foo");
            a.SetTraceparent($"00-{TraceId}-{ParenSpanId}-00");

            Assert.IsTrue(a.IsW3CActivity());
            Assert.AreEqual(TraceId, a.Tags.SingleOrDefault(t => t.Key == W3CConstants.TraceIdTag).Value);
            Assert.AreEqual(ParenSpanId, a.Tags.SingleOrDefault(t => t.Key == W3CConstants.ParentSpanIdTag).Value);
            Assert.IsNotNull(a.Tags.SingleOrDefault(t => t.Key == W3CConstants.SpanIdTag));
            Assert.AreEqual(16, a.Tags.Single(t => t.Key == W3CConstants.SpanIdTag).Value.Length);
            Assert.AreEqual(W3CConstants.TraceFlagRecordedAndNotRequested, a.Tags.SingleOrDefault(t => t.Key == W3CConstants.SampledTag).Value);
            Assert.AreEqual(W3CConstants.DefaultVersion, a.Tags.SingleOrDefault(t => t.Key == W3CConstants.VersionTag).Value);

            Assert.AreEqual(TraceId, a.GetTraceId());
            Assert.AreEqual(ParenSpanId, a.GetParentSpanId());
            Assert.IsNotNull(a.GetSpanId());
            Assert.AreEqual(a.Tags.Single(t => t.Key == W3CConstants.SpanIdTag).Value, a.GetSpanId());
            Assert.AreEqual($"{W3CConstants.DefaultVersion}-{TraceId}-{a.GetSpanId()}-{W3CConstants.TraceFlagRecordedAndNotRequested}", a.GetTraceparent());
            Assert.IsNull(a.GetTracestate());
        }

        [TestMethod]
        public void UpdateContextWithoutParent()
        {
            var a = new Activity("foo");

            Assert.IsFalse(a.IsW3CActivity());

            a.UpdateContextOnActivity();
            Assert.IsTrue(a.IsW3CActivity());
            Assert.IsNotNull(a.GetTraceId());
            Assert.IsNotNull(a.GetSpanId());
            Assert.IsNull(a.GetParentSpanId());
            Assert.IsNotNull(a.GetSpanId());

            Assert.AreEqual($"00-{a.GetTraceId()}-{a.GetSpanId()}-02", a.GetTraceparent());
            Assert.IsNull(a.GetTracestate());
        }

        [TestMethod]
        public void UpdateContextWithParent()
        {
            var parent = new Activity("foo").Start();
            parent.SetTraceparent($"00-{TraceId}-{ParenSpanId}-01");
            parent.SetTracestate("some=state");
            var child = new Activity("bar").Start();
            child.UpdateContextOnActivity();

            Assert.IsTrue(child.IsW3CActivity());
            Assert.AreEqual(TraceId, child.GetTraceId());
            Assert.AreEqual(parent.GetSpanId(), child.GetParentSpanId());
            Assert.AreEqual($"{W3CConstants.DefaultVersion}-{TraceId}-{child.GetSpanId()}-{W3CConstants.TraceFlagRecordedAndRequested}", child.GetTraceparent());
            Assert.AreEqual(parent.GetTracestate(), child.GetTracestate());
        }

        [TestMethod]
        public void SetTraceState()
        {
            var a = new Activity("foo").Start();
            a.SetTracestate("some=state");
            Assert.AreEqual("some=state", a.GetTracestate());
        }

        [TestMethod]
        public void UnsupportedVersionsAreIgnored()
        {
            var a = new Activity("foo").Start();
            a.SetTraceparent($"12-{TraceId}-{ParenSpanId}-00");

            var b = new Activity("bar").Start();
            b.SetTraceparent($"ff-{TraceId}-{ParenSpanId}-00");
            
            Assert.AreEqual($"00-{TraceId}-{a.GetSpanId()}-02", a.GetTraceparent());
            Assert.AreEqual($"00-{TraceId}-{b.GetSpanId()}-02", b.GetTraceparent());
        }

        [TestMethod]
        public void RequestedFlagIsRespected()
        {
            var requestedParents = new[] { "01", "03", "05", "ff" };
            var notRequestedParents = new[] { "00", "02", "04", "fe" };

            foreach (var req in requestedParents)
            {
                var a = new Activity("foo").Start();
                a.SetTraceparent($"00-{TraceId}-{ParenSpanId}-{req}");
                Assert.AreEqual($"00-{TraceId}-{a.GetSpanId()}-03", a.GetTraceparent(), req);
            }

            foreach (var notReq in notRequestedParents)
            {
                var a = new Activity("foo").Start();
                a.SetTraceparent($"00-{TraceId}-{ParenSpanId}-{notReq}");
                Assert.AreEqual($"00-{TraceId}-{a.GetSpanId()}-02", a.GetTraceparent(), notReq);
            }
        }

#pragma warning restore 612, 618
    }
}
