namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    class SelfDiagnosticsEventListenerTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SelfDiagnosticsEventListener_constructor_Invalid_Input()
        {
            // no configRefresher object
            _ = new SelfDiagnosticsEventListener(EventLevel.Error, null);
        }

        [TestMethod]
        public void SelfDiagnosticsEventListener_EventSourceSetup_LowerSeverity()
        {
            var fileHandlerMock = new Mock<MemoryMappedFileHandler>();
            var listener = new SelfDiagnosticsEventListener(EventLevel.Error, fileHandlerMock.Object);

            // Emitting a Verbose event. Or any EventSource event with lower severity than Error.
            CoreEventSource.Log.OperationIsNullWarning();
            fileHandlerMock.Verify(fileHandler => fileHandler.Write(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Never());
        }

        [TestMethod]
        public void SelfDiagnosticsEventListener_EventSourceSetup_HigherSeverity()
        {
            var fileHandlerMock = new Mock<MemoryMappedFileHandler>();
            fileHandlerMock.Setup(fileHandler => fileHandler.Write(It.IsAny<byte[]>(), It.IsAny<int>()));
            var listener = new SelfDiagnosticsEventListener(EventLevel.Error, fileHandlerMock.Object);

            // Emitting an Error event. Or any EventSource event with higher than or equal to to Error severity.
            CoreEventSource.Log.InvalidOperationToStopError();
            fileHandlerMock.Verify(fileHandler => fileHandler.Write(It.IsAny<byte[]>(), It.IsAny<int>()));
        }

        [TestMethod]
        public void SelfDiagnosticsEventListener_DateTimeGetBytes()
        {
            var fileHandlerMock = new Mock<MemoryMappedFileHandler>();
            var listener = new SelfDiagnosticsEventListener(EventLevel.Error, fileHandlerMock.Object);

            // Check DateTimeKind of Utc, Local, and Unspecified
            DateTime[] datetimes = new DateTime[]
            {
                DateTime.SpecifyKind(DateTime.Parse("1996-12-01T14:02:31.1234567-08:00"), DateTimeKind.Utc),
                DateTime.SpecifyKind(DateTime.Parse("1996-12-01T14:02:31.1234567-08:00"), DateTimeKind.Local),
                DateTime.SpecifyKind(DateTime.Parse("1996-12-01T14:02:31.1234567-08:00"), DateTimeKind.Unspecified),
                DateTime.UtcNow,
                DateTime.Now,
            };

            // Expect to match output string from DateTime.ToString("O")
            string[] expected = new string[datetimes.Length];
            for (int i = 0; i < datetimes.Length; i++)
            {
                expected[i] = datetimes[i].ToString("O");
            }

            byte[] buffer = new byte[40 * datetimes.Length];
            int pos = 0;

            // Get string after DateTimeGetBytes() write into a buffer
            string[] results = new string[datetimes.Length];
            for (int i = 0; i < datetimes.Length; i++)
            {
                int len = listener.DateTimeGetBytes(datetimes[i], buffer, pos);
                results[i] = Encoding.Default.GetString(buffer, pos, len);
                pos += len;
            }

            Assert.AreEqual(expected, results);
        }
    }
}
