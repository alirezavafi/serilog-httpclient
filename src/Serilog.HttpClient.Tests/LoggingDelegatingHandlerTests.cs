using System.Net;
using System.Net.Http.Headers;
using Moq;
using Moq.Protected;
using Serilog.Events;
using Serilog.HttpClient.Models;
using Serilog.HttpClient.Tests.Support;
using Serilog.Sinks.TestCorrelator;

namespace Serilog.HttpClient.Tests;

public class LoggingDelegatingHandlerTests
{
    private Mock<HttpMessageHandler> _msgHandler = new Mock<HttpMessageHandler>();

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
            Assert.Equal("HTTP Client {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms", logEvent.MessageTemplate.Text);
            Assert.Equal(LogEventLevel.Information, logEvent.Level);
            Assert.Null(logEvent.Exception);
            var request = ((StructureValue)((StructureValue)logEvent.Properties["Context"]).Properties.First(x => x.Name == nameof(HttpClientContext.Request)).Value).Properties.ToDictionary(x => x.Name);
            var response = ((StructureValue)((StructureValue)logEvent.Properties["Context"]).Properties.First(x => x.Name == nameof(HttpClientContext.Response)).Value).Properties.ToDictionary(x => x.Name);
            Assert.Equal("POST", request[nameof(HttpRequestInfo.Method)].Value.ToScalar());
            Assert.Equal("https", request[nameof(HttpRequestInfo.Scheme)].Value.ToScalar());
            Assert.Equal("example.com", request[nameof(HttpRequestInfo.Host)].Value.ToScalar());
            Assert.Equal("/path", request[nameof(HttpRequestInfo.Path)].Value.ToScalar());
            Assert.Equal("?query=1", request[nameof(HttpRequestInfo.QueryString)].Value.ToScalar());
            Assert.Equal("this is the request body", request[nameof(HttpRequestInfo.BodyString)].Value.ToScalar());
            Assert.Null(request[nameof(HttpRequestInfo.Body)].Value.ToScalar());
            Assert.Equal("Referer", request[nameof(HttpRequestInfo.Headers)].Value.ToDictionary().First().Key.ToScalar());
            Assert.Equal("https://example.com/referrer", request[nameof(HttpRequestInfo.Headers)].Value.ToDictionary().First().Value.ToScalar());

            Assert.Equal(200 , response[nameof(HttpResponseInfo.StatusCode)].Value.ToScalar());
            Assert.True((bool)response[nameof(HttpResponseInfo.IsSucceed)].Value.ToScalar());
            Assert.IsType<double>(response[nameof(HttpResponseInfo.ElapsedMilliseconds)].Value.ToScalar());
            Assert.Equal("this is the response body", response[nameof(HttpResponseInfo.BodyString)].Value.ToScalar());
            Assert.Null(request[nameof(HttpResponseInfo.Body)].Value.ToScalar());
            Assert.Equal("ETag", response[nameof(HttpResponseInfo.Headers)].Value.ToDictionary().First().Key.ToScalar());
            Assert.Equal("*", response[nameof(HttpResponseInfo.Headers)].Value.ToDictionary().First().Value.ToScalar());
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
            GetLogMessageAndProperties = c => new LogEntryParameters()
            {
                MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms",
                AdditionalProperties = new Dictionary<string, object>()
                {
                    { "RequestMethod", c.Request.Method },
                    { "RequestScheme", c.Request.Scheme },
                    { "RequestHost", c.Request.Host },
                    { "RequestPath", c.Request.Path },
                    { "RequestQueryString", c.Request.QueryString },
                    { "RequestQuery", c.Request.Query },
                    { "RequestBodyString", c.Request.BodyString },
                    { "RequestBody", c.Request.Body },
                    { "RequestHeaders", c.Request.Headers },
                    { "StatusCode", c.Response.StatusCode ?? 0 },
                    { "IsSucceed", c.Response.IsSucceed },
                    { "ElapsedMilliseconds", c.Response.ElapsedMilliseconds },
                    { "ResponseBodyString", c.Response.BodyString },
                    { "ResponseBody", c.Response.Body },
                    { "ResponseHeaders", c.Response.Headers }
                }
            }
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
            Assert.Equal("HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms",
                logEvent.MessageTemplate.Text);
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
            Assert.Equal("https://example.com/referrer",
                logEvent.Properties["RequestHeaders"].ToDictionary().First().Value.ToScalar());

            Assert.Equal(200, logEvent.Properties["StatusCode"].ToScalar());
            Assert.True((bool)logEvent.Properties["IsSucceed"].ToScalar());
            Assert.IsType<double>(logEvent.Properties["ElapsedMilliseconds"].ToScalar());
            Assert.Equal("this is the response body", logEvent.Properties["ResponseBodyString"].ToScalar());
            Assert.Null(logEvent.Properties["ResponseBody"].ToScalar());
            Assert.Equal("ETag", logEvent.Properties["ResponseHeaders"].ToDictionary().First().Key.ToScalar());
            Assert.Equal("*",
                logEvent.Properties["ResponseHeaders"].ToDictionary().First().Value.ToScalar());
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
            GetLogMessageAndProperties = c => new LogEntryParameters()
            {
                MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms",
                AdditionalProperties = new Dictionary<string, object>()
                {
                    { "RequestMethod", c.Request.Method },
                    { "RequestScheme", c.Request.Scheme },
                    { "RequestHost", c.Request.Host },
                    { "RequestPath", c.Request.Path },
                    { "RequestQueryString", c.Request.QueryString },
                    { "RequestQuery", c.Request.Query },
                    { "RequestBodyString", c.Request.BodyString },
                    { "RequestBody", c.Request.Body },
                    { "RequestHeaders", c.Request.Headers },
                    { "StatusCode", c.Response.StatusCode ?? 0 },
                    { "IsSucceed", c.Response.IsSucceed },
                    { "ElapsedMilliseconds", c.Response.ElapsedMilliseconds },
                    { "ResponseBodyString", c.Response.BodyString },
                    { "ResponseBody", c.Response.Body },
                    { "ResponseHeaders", c.Response.Headers }
                }
            }
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

            var responseText = logEvent.Properties["RequestBodyString"].ToScalar().ToString();
            Assert.Contains("\"Authorization\": \"*** MASKED ***\"",
                responseText);
            Assert.Contains("\"Password\": \"*** MASKED ***\"",
                responseText);
            Assert.Equal("Authorization", logEvent.Properties["RequestHeaders"].ToDictionary().First().Key.ToScalar());
            Assert.Equal("*** MASKED ***",
                logEvent.Properties["RequestHeaders"].ToDictionary().First().Value.ToScalar());

            Assert.Contains("\"Password\": \"*** MASKED ***\"",
                (string)logEvent.Properties["ResponseBodyString"].ToScalar());
            Assert.Contains("\"Token\": \"*** MASKED ***\"",
                (string)logEvent.Properties["ResponseBodyString"].ToScalar());
            Assert.Equal("WWW-Authenticate",
                logEvent.Properties["ResponseHeaders"].ToDictionary().First().Key.ToScalar());
            Assert.Equal("*** MASKED ***",
                logEvent.Properties["ResponseHeaders"].ToDictionary().First().Value.ToScalar());
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
            .WriteTo.TestCorrelator()
            .CreateLogger();

        options.Logger = Log.Logger;

        var loggingHandler = new LoggingDelegatingHandler(options);
        loggingHandler.InnerHandler = _msgHandler.Object;
        var client = new System.Net.Http.HttpClient(loggingHandler);
        return client;
    }
}