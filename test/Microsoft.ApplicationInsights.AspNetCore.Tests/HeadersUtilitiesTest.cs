namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using DiagnosticListeners;
    using Xunit;

    public class HeadersUtilitiesTest
    {
        [Fact]
        public void ShouldReturnHeaderValueWhenKeyExists()
        {
            List<string> headers = new List<string>() {
                "Key = Value"
            };

            string expectedKey = "Value";
            Assert.Equal(expectedKey, HeadersUtilities.GetHeaderKeyValue(headers, "Key"));
        }

        [Fact]
        public void ShouldReturnNullWhenKeyNotExists()
        {
            List<string> headers = new List<string>() {
                "Key = Value"
            };
            Assert.Null(HeadersUtilities.GetHeaderKeyValue(headers, "Non-Exist-Key"));
        }

        [Fact]
        public void ShouldReturnHeadersWhenNoHeaderValues()
        {
            IEnumerable<string> newHeaders = HeadersUtilities.SetHeaderKeyValue(null, "Key", "Value");

            Assert.NotNull(newHeaders);
            Assert.Equal(1, newHeaders.Count());
            Assert.Equal("Key=Value", newHeaders.First());
        }

        [Fact]
        public void ShouldAppendHeaders()
        {
            IEnumerable<string> existing = new List<string>() { "ExistKey=ExistValue" };
            IEnumerable<string> result = HeadersUtilities.SetHeaderKeyValue(existing, "NewKey", "NewValue");

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.NotNull(result.FirstOrDefault(headerValue => headerValue.Equals("ExistKey=ExistValue")));
            Assert.NotNull(result.FirstOrDefault(headerValue => headerValue.Equals("NewKey=NewValue")));
        }

        [Fact]
        public void ShouldUpdateExistingHeader()
        {
            IEnumerable<string> existing = new List<string>() { "ExistKey=ExistValue", "NoiseKey=NoiseValue" };
            IEnumerable<string> result = HeadersUtilities.SetHeaderKeyValue(existing, "ExistKey", "NewValue");

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Null(result.FirstOrDefault(headerValue => headerValue.Equals("ExistKey=ExistValue")));
            Assert.NotNull(result.FirstOrDefault(headerValue => headerValue.Equals("ExistKey=NewValue")));
            Assert.NotNull(result.FirstOrDefault(headerValue => headerValue.Equals("NoiseKey=NoiseValue")));
        }
    }
}
