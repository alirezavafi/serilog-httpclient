using System.Net;
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
            Assert.Equal("HTTP {RequestMethod} {RequestUri} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms", logEvent.MessageTemplate.Text);
            Assert.Equal(LogEventLevel.Information, logEvent.Level);
            Assert.Null(logEvent.Exception);
            Assert.Equal("POST", logEvent.Properties["RequestMethod"].ToScalar());
            Assert.Equal("https", logEvent.Properties["RequestScheme"].ToScalar());
            Assert.Equal("example.com", logEvent.Properties["RequestHost"].ToScalar());
            Assert.Equal("/path", logEvent.Properties["RequestPath"].ToScalar());
            Assert.Equal("?query=1", logEvent.Properties["RequestQueryString"].ToScalar());
            Assert.Equal("this is the request body", logEvent.Properties["RequestBodyString"].ToScalar());
            Assert.Null(logEvent.Properties["RequestBody"].ToScalar());
            Assert.Equal("Referer", logEvent.Properties["RequestHeaders"].ToDictionary().First().Key.ToScalar());
            Assert.Equal("https://example.com/referrer", logEvent.Properties["RequestHeaders"].ToDictionary().First().Value.ToSequence().First().ToScalar());

            Assert.Equal("OK", logEvent.Properties["StatusCode"].ToScalar());
            Assert.True((bool)logEvent.Properties["IsSucceed"].ToScalar());
            Assert.IsType<double>(logEvent.Properties["ElapsedMilliseconds"].ToScalar());
            Assert.Equal("this is the response body", logEvent.Properties["ResponseBodyString"].ToScalar());
            Assert.Null(logEvent.Properties["ResponseBody"].ToScalar());
            Assert.Equal("ETag", logEvent.Properties["ResponseHeaders"].ToDictionary().First().Key.ToScalar());
            Assert.Equal("*", logEvent.Properties["ResponseHeaders"].ToDictionary().First().Value.ToSequence().First().ToScalar());
        }
    }

    [Fact]
    public void Test_Log_Request_With_Masking()
    {
        mockResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"Password\": false, \"Token\": \"abcdef\"}"),
            Headers = {
                WwwAuthenticate = { new AuthenticationHeaderValue("Bearer") }
            }
        });

        var client = createHttpClient(new RequestLoggingOptions
        {
            ResponseBodyLogMode = LogMode.LogAll,
            MaskedProperties = { "password", "token", "authorization", "*authenticate*" }
        });

        using (TestCorrelator.CreateContext())
        {
            client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://example.com/path"),
                Content = new StringContent("{\"Authorization\": 1234, \"Password\": \"xyz\"}"),
                Headers = {
                    Authorization = new AuthenticationHeaderValue("Bearer", "abcdef")
                }
            });

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext();
            Assert.Single(logEvents);

            var logEvent = logEvents.First();

            Assert.Contains("\"Authorization\":\"*** MASKED ***\"", (string)logEvent.Properties["RequestBodyString"].ToScalar());
            Assert.Contains("\"Password\":\"*** MASKED ***\"", (string)logEvent.Properties["RequestBodyString"].ToScalar());
            Assert.Equal("Authorization", logEvent.Properties["RequestHeaders"].ToDictionary().First().Key.ToScalar());
            Assert.Equal("*** MASKED ***", logEvent.Properties["RequestHeaders"].ToDictionary().First().Value.ToSequence().First().ToScalar());

            Assert.Contains("\"Password\":\"*** MASKED ***\"", (string)logEvent.Properties["ResponseBodyString"].ToScalar());
            Assert.Contains("\"Token\":\"*** MASKED ***\"", (string)logEvent.Properties["ResponseBodyString"].ToScalar());
            Assert.Equal("WWW-Authenticate", logEvent.Properties["ResponseHeaders"].ToDictionary().First().Key.ToScalar());
            Assert.Equal("*** MASKED ***", logEvent.Properties["ResponseHeaders"].ToDictionary().First().Value.ToSequence().First().ToScalar());

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
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.TestCorrelator()
            .CreateLogger();

        options.Logger = Log.Logger;

        var loggingHandler = new LoggingDelegatingHandler(options);
        loggingHandler.InnerHandler = _msgHandler.Object;
        var client = new System.Net.Http.HttpClient(loggingHandler);
        return client;
    }
}
