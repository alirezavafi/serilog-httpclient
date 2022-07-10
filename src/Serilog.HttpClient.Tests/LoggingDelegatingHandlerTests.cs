using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Moq;
using Moq.Protected;
using Serilog.Events;
using Serilog.HttpClient.Tests.Support;
using Serilog.Sinks.TestCorrelator;

namespace Serilog.HttpClient.Tests;

public class LoggingDelegatingHandlerTests
{
    private Mock<HttpMessageHandler> _msgHandler = new Mock<HttpMessageHandler>();

    [Fact]
    public void Test_Log_Request()
    {
        mockResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("this is the response body"),
            Headers = {
                    ETag = EntityTagHeaderValue.Any
                }
        });

        var client = createHttpClient(new RequestLoggingOptions
        {
            ResponseBodyLogMode = LogMode.LogAll
        });

        using (TestCorrelator.CreateContext())
        {
            client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://example.com/path?query=1"),
                Content = new StringContent("this is the request body"),
                Headers = {
                    Referrer = new Uri("https://example.com/referrer")
                }
            });

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext();
            Assert.Single(logEvents);

            var logEvent = logEvents.First();
            Assert.Equal("HTTP Client request {RequestMethod} {RequestUri} completed in {ElapsedMilliseconds:0.0000}ms", logEvent.MessageTemplate.Text);
            Assert.Equal(LogEventLevel.Information, logEvent.Level);
            Assert.Null(logEvent.Exception);
            Assert.Equal("POST", logEvent.Properties["RequestMethod"].LiteralValue());
            Assert.Equal("https", logEvent.Properties["RequestScheme"].LiteralValue());
            Assert.Equal("example.com", logEvent.Properties["RequestHost"].LiteralValue());
            Assert.Equal("/path", logEvent.Properties["RequestPath"].LiteralValue());
            Assert.Equal("?query=1", logEvent.Properties["RequestQueryString"].LiteralValue());
            Assert.Equal("this is the request body", logEvent.Properties["RequestBodyString"].LiteralValue());
            Assert.Null(logEvent.Properties["RequestBody"].LiteralValue());
            Assert.Equal(
                new Dictionary<string, string[]>()
                {
                    { "Referer", new string[] {"https://example.com/referrer"} }
                },
                logEvent.Properties["RequestHeaders"].LiteralValue()
            );

            Assert.Equal("OK", logEvent.Properties["StatusCode"].LiteralValue());
            Assert.True((bool)logEvent.Properties["IsSucceed"].LiteralValue());
            Assert.IsType<double>(logEvent.Properties["ElapsedMilliseconds"].LiteralValue());
            Assert.Equal("this is the response body", logEvent.Properties["ResponseBodyString"].LiteralValue());
            Assert.Null(logEvent.Properties["ResponseBody"].LiteralValue());
            Assert.Equal(
                new Dictionary<string, string[]>()
                {
                    { "ETag", new string[] {"*"} }
                },
                logEvent.Properties["ResponseHeaders"].LiteralValue()
            );
        }
    }

    private void mockResponse(HttpResponseMessage response)
    {
        var mockedProtected = _msgHandler.Protected();
        var setupApiRequest = mockedProtected.Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
        var apiMockedResponse = setupApiRequest.ReturnsAsync(response);
        apiMockedResponse.Verifiable();
    }

    private System.Net.Http.HttpClient createHttpClient(RequestLoggingOptions options)
    {
        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();

        options.Logger = Log.Logger;

        var loggingHandler = new LoggingDelegatingHandler(options, _msgHandler.Object);
        var client = new System.Net.Http.HttpClient(loggingHandler);
        return client;
    }
}
