using System.Net;
using System.Net.Http.Headers;
using Moq;
using Moq.Protected;
using Serilog.Events;
using Serilog.HttpClient.Extensions;
using Serilog.HttpClient.Models;
using Serilog.HttpClient.Tests.Support;
using Serilog.Sinks.TestCorrelator;

namespace Serilog.HttpClient.Tests;

public class LoggingDelegatingHandlerTests
{
    private readonly Mock<HttpMessageHandler> _msgHandler = new();

    [Fact]
    public void Test_Log_Request()
    {
        MockResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("this is the response body"),
            Headers =
            {
                ETag = EntityTagHeaderValue.Any
            }
        });

        var client = CreateHttpClient(new RequestLoggingOptions
        {
            ResponseBodyLogMode = LogMode.LogAll,
        });

        using (TestCorrelator.CreateContext())
        {
            client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://example.com/path?query=1"),
                Content = new StringContent("this is the request body"),
                Headers =
                {
                    Referrer = new Uri("https://example.com/referrer")
                }
            });

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext();
            Assert.Single(logEvents);

            var logEvent = logEvents.First();
            Assert.Equal("HTTP Client Request Completed {@Context}", logEvent.MessageTemplate.Text);
            Assert.Equal(LogEventLevel.Information, logEvent.Level);
            Assert.Null(logEvent.Exception);
            var request = ((StructureValue)((StructureValue)logEvent.Properties["Context"]).Properties.First(x => x.Name == nameof(HttpClientContext.Request)).Value).Properties.ToDictionary(x => x.Name);
            var response = ((StructureValue)((StructureValue)logEvent.Properties["Context"]).Properties.First(x => x.Name == nameof(HttpClientContext.Response)).Value).Properties.ToDictionary(x => x.Name);
            Assert.Equal("POST", request[nameof(HttpClientRequestContext.Method)].Value.ToScalar());
            Assert.Equal("https", request[nameof(HttpClientRequestContext.Scheme)].Value.ToScalar());
            Assert.Equal("example.com", request[nameof(HttpClientRequestContext.Host)].Value.ToScalar());
            Assert.Equal("/path", request[nameof(HttpClientRequestContext.Path)].Value.ToScalar());
            Assert.Equal("?query=1", request[nameof(HttpClientRequestContext.QueryString)].Value.ToScalar());
            Assert.Equal("this is the request body", request[nameof(HttpClientRequestContext.BodyString)].Value.ToScalar());
            Assert.Null(request[nameof(HttpClientRequestContext.Body)].Value.ToScalar());
            Assert.Equal("Referer", request[nameof(HttpClientRequestContext.Headers)].Value.ToDictionary().First().Key.ToScalar());
            Assert.Equal("https://example.com/referrer", request[nameof(HttpClientRequestContext.Headers)].Value.ToDictionary().First().Value.ToScalar());

            Assert.Equal(200 , response[nameof(HttpClientResponseContext.StatusCode)].Value.ToScalar());
            Assert.True((bool)response[nameof(HttpClientResponseContext.IsSucceed)].Value.ToScalar());
            Assert.IsType<double>(response[nameof(HttpClientResponseContext.ElapsedMilliseconds)].Value.ToScalar());
            Assert.Equal("this is the response body", response[nameof(HttpClientResponseContext.BodyString)].Value.ToScalar());
            Assert.Null(request[nameof(HttpClientResponseContext.Body)].Value.ToScalar());
            Assert.Equal("ETag", response[nameof(HttpClientResponseContext.Headers)].Value.ToDictionary().First().Key.ToScalar());
            Assert.Equal("*", response[nameof(HttpClientResponseContext.Headers)].Value.ToDictionary().First().Value.ToScalar());
        }
    }

    [Fact]
    public void Test_Log_Request_With_Customized_Log_Entry()
    {
        MockResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("this is the response body"),
            Headers =
            {
                ETag = EntityTagHeaderValue.Any
            }
        });

        var client = CreateHttpClient(new RequestLoggingOptions
        {
            ResponseBodyLogMode = LogMode.LogAll,
            MessageTemplate =  "HTTP {RequestMethod} {RequestUri} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms",
            GetMessageTemplateProperties = (c, l) => new[]
            {
                new LogEventProperty("RequestMethod", new ScalarValue(c.Request.Method)),
                new LogEventProperty("RequestUri", new ScalarValue(c.Request.Url)),
                new LogEventProperty("StatusCode", new ScalarValue(c.Response.StatusCode)),
                new LogEventProperty("ElapsedMilliseconds", new ScalarValue(c.Response.ElapsedMilliseconds))
            },
        });

        using (TestCorrelator.CreateContext())
        {
            client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://example.com/path?query=1"),
                Content = new StringContent("this is the request body"),
                Headers =
                {
                    Referrer = new Uri("https://example.com/referrer")
                }
            });

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext();
            Assert.Single(logEvents);

            var logEvent = logEvents.First();
            Assert.Equal("HTTP {RequestMethod} {RequestUri} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms",
                logEvent.MessageTemplate.Text);
            Assert.Equal(LogEventLevel.Information, logEvent.Level);
            Assert.Null(logEvent.Exception);

            Assert.Equal("POST", logEvent.Properties["RequestMethod"].ToScalar());
            Assert.Equal("https://example.com/path?query=1", logEvent.Properties["RequestUri"].ToScalar());
            Assert.Equal(200, logEvent.Properties["StatusCode"].ToScalar());
            Assert.IsType<double>(logEvent.Properties["ElapsedMilliseconds"].ToScalar());
        }
    }

    [Fact]
    public void Test_Log_Request_With_Masking()
    {
        MockResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"Password\": false, \"Token\": \"abcdef\"}"),
            Headers =
            {
                WwwAuthenticate = { new AuthenticationHeaderValue("Bearer") }
            }
        });

        var client = CreateHttpClient(new RequestLoggingOptions
        {
            ResponseBodyLogMode = LogMode.LogAll,
            MaskedProperties = { "password", "token", "authorization", "*authenticate*" },
        });

        using (TestCorrelator.CreateContext())
        {
            client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://example.com/path"),
                Content = new StringContent("{\"Authorization\": 1234, \"Password\": \"xyz\"}"),
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", "abcdef")
                }
            });

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext();
            Assert.Single(logEvents);

            var logEvent = logEvents.First();
            var context = (StructureValue)logEvent.Properties["Context"];
            var req = context.Properties.First(x => x.Name == nameof(HttpClientContext.Request));
            var resp = context.Properties.First(x => x.Name == nameof(HttpClientContext.Response));
            var requestHeaders = ((StructureValue)req.Value).Properties.First(x => x.Name == nameof(HttpClientRequestContext.Headers)).Value.ToDictionary(); ;
            var authHeader = requestHeaders.First(x => x.Key.ToScalar().ToString() == "Authorization").Value.ToString();
            var responseBody = ((StructureValue)resp.Value).Properties.First(x => x.Name == nameof(HttpClientResponseContext.Body)).Value;
            var passwordValue = ((StructureValue)responseBody).Properties.First(x => x.Name == "Password").Value.ToScalar()
                .ToString();
            Assert.Contains("\"*** MASKED ***\"",  authHeader);
            Assert.Contains("*** MASKED ***",  passwordValue);
        }
    }

    private void MockResponse(HttpResponseMessage response)
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

    private System.Net.Http.HttpClient CreateHttpClient(RequestLoggingOptions options)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .AddJsonDestructuringPolicies()
            .WriteTo.TestCorrelator()
            .CreateLogger();

        options.Logger = Log.Logger;

        var loggingHandler = new LoggingDelegatingHandler(options);
        loggingHandler.InnerHandler = _msgHandler.Object;
        var client = new System.Net.Http.HttpClient(loggingHandler);
        return client;
    }
}