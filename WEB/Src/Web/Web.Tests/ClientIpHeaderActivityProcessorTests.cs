namespace Microsoft.ApplicationInsights.Web.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Xunit;

    public class ClientIpHeaderActivityProcessorTests : ActivityProcessorTestBase
    {
        [Fact]
        public void Constructor_SetsDefaultClientIpHeader()
        {
            // Arrange & Act
            var processor = new ClientIpHeaderActivityProcessor();

            // Assert
            Assert.Contains("X-Forwarded-For", processor.HeaderNames);
        }

        [Fact]
        public void Constructor_SetsDefaultUseFirstIp()
        {
            // Arrange & Act
            var processor = new ClientIpHeaderActivityProcessor();

            // Assert
            Assert.True(processor.UseFirstIp);
        }

        [Fact]
        public void Constructor_SetsDefaultHeadersSeparator()
        {
            // Arrange & Act
            var processor = new ClientIpHeaderActivityProcessor();

            // Assert
            Assert.Equal(",", processor.HeaderValueSeparators);
        }

        [Fact]
        public void OnEnd_DoesNotThrowWhenActivityIsNull()
        {
            // Arrange
            var processor = new ClientIpHeaderActivityProcessor();

            // Act & Assert
            processor.OnEnd(null);
        }

        [Fact]
        public void OnEnd_DoesNotThrowWhenHttpContextIsNull()
        {
            // Arrange
            SetupTracerProvider(new ClientIpHeaderActivityProcessor());

            // Act & Assert - Should not throw
            using var activity = StartTestActivity();
        }

        [Fact]
        public void OnEnd_SetsClientIpToUserHostAddressIfNoHeadersInRequest()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            SetupTracerProvider(new ClientIpHeaderActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var clientIp = activity.GetTagItem("client.address");
            Assert.NotNull(clientIp);
            Assert.Equal("127.0.0.1", clientIp.ToString());
        }

        [Fact]
        public void OnEnd_DoesNotOverrideExistingClientIp()
        {
            // Arrange
            var context = HttpModuleHelper.GetFakeHttpContext();
            SetupTracerProvider(new ClientIpHeaderActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
                activity.SetTag("client.address", "10.10.10.10");
            } // Activity ends, processor should not override

            // Assert
            var clientIp = activity.GetTagItem("client.address");
            Assert.Equal("10.10.10.10", clientIp.ToString());
        }

        [Theory]
        [InlineData("1.2.3.4", "1.2.3.4")]
        [InlineData("[::1]:80", "::1")]
        [InlineData("0:0:0:0:0:0:0:1", "::1")]
        public void OnEnd_SetsClientIpFromXForwardedForHeader(string headerValue, string expectedIp)
        {
            // Arrange
            var headers = new Dictionary<string, string> { { "X-Forwarded-For", headerValue } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(new ClientIpHeaderActivityProcessor());

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var clientIp = activity.GetTagItem("client.address");
            Assert.NotNull(clientIp);
            Assert.Equal(expectedIp, clientIp.ToString());
        }

        [Fact]
        public void OnEnd_UsesFirstIpFromCommaSeparatedList()
        {
            // Arrange
            var processor = new ClientIpHeaderActivityProcessor();
            processor.UseFirstIp = true;
            
            var headers = new Dictionary<string, string> { { "X-Forwarded-For", "1.2.3.4, 5.6.7.8" } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(processor);

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var clientIp = activity.GetTagItem("client.address");
            Assert.NotNull(clientIp);
            Assert.Equal("1.2.3.4", clientIp.ToString());
        }

        [Fact]
        public void OnEnd_UsesLastIpFromCommaSeparatedList()
        {
            // Arrange
            var processor = new ClientIpHeaderActivityProcessor();
            processor.UseFirstIp = false;
            
            var headers = new Dictionary<string, string> { { "X-Forwarded-For", "1.2.3.4, 5.6.7.8" } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(processor);

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var clientIp = activity.GetTagItem("client.address");
            Assert.NotNull(clientIp);
            Assert.Equal("5.6.7.8", clientIp.ToString());
        }

        [Fact]
        public void OnEnd_SupportsCustomHeaderNames()
        {
            // Arrange
            var processor = new ClientIpHeaderActivityProcessor();
            processor.HeaderNames.Clear();
            processor.HeaderNames.Add("Custom-IP-Header");
            
            var headers = new Dictionary<string, string> { { "Custom-IP-Header", "192.168.1.1" } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(processor);

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var clientIp = activity.GetTagItem("client.address");
            Assert.NotNull(clientIp);
            Assert.Equal("192.168.1.1", clientIp.ToString());
        }

        [Fact]
        public void OnEnd_ChecksMultipleHeadersInOrder()
        {
            // Arrange
            var processor = new ClientIpHeaderActivityProcessor();
            processor.HeaderNames.Clear();
            processor.HeaderNames.Add("Header1");
            processor.HeaderNames.Add("Header2");
            
            // Only Header2 exists
            var headers = new Dictionary<string, string> { { "Header2", "192.168.1.1" } };
            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            SetupTracerProvider(processor);

            // Act
            Activity activity;
            using (activity = StartTestActivity())
            {
                Assert.NotNull(activity);
            } // Activity ends

            // Assert
            var clientIp = activity.GetTagItem("client.address");
            Assert.NotNull(clientIp);
            Assert.Equal("192.168.1.1", clientIp.ToString());
        }
    }
}
