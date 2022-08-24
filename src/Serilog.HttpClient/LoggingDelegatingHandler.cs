// Copyright 2019 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Context;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.HttpClient.Extensions;
using Serilog.HttpClient.Models;

namespace Serilog.HttpClient
{
    public class LoggingDelegatingHandler : DelegatingHandler
    {
        private readonly RequestLoggingOptions _options;
        private readonly ILogger _logger;

        public LoggingDelegatingHandler(
            RequestLoggingOptions options,
            HttpMessageHandler httpMessageHandler = default, bool forHttpClientFactory = false)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = options.Logger?.ForContext<LoggingDelegatingHandler>() ?? Serilog.Log.Logger.ForContext<LoggingDelegatingHandler>();

#if NETCOREAPP3_1_OR_GREATER
            if (!forHttpClientFactory)
            {
               InnerHandler = httpMessageHandler ?? new SocketsHttpHandler();
            }
#else
            if (!forHttpClientFactory)
            {
                InnerHandler = httpMessageHandler ?? new HttpClientHandler();
            }
#endif
            
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var start = Stopwatch.GetTimestamp();
            try
            {
                var resp = await base.SendAsync(request, cancellationToken);
                var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());
                await LogRequest(request, resp, elapsedMs, null);
                return resp;
            }
            catch (Exception ex)
            {
                var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());
                await LogRequest(request, null, elapsedMs, ex);
                throw;
            }
        }

        static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }

        private async Task LogRequest(HttpRequestMessage req, HttpResponseMessage resp, double elapsedMs,
            Exception ex)
        {
            var level = _options.GetLevel(req, resp, elapsedMs, ex);
            if (!_logger.IsEnabled(level)) return;

            var requestBodyText = string.Empty;
            var responseBodyText = string.Empty;

            var isRequestOk = !(resp != null && (int)resp.StatusCode >= 400 || ex != null);
            if (_options.LogMode == LogMode.LogAll ||
                (!isRequestOk && _options.LogMode == LogMode.LogFailures))
            {
                object requestBody = null;
                if ((_options.RequestBodyLogMode == LogMode.LogAll ||
                     (!isRequestOk && _options.RequestBodyLogMode == LogMode.LogFailures)))
                {
                    if (req.Content != null)
                        requestBodyText = await req.Content.ReadAsStringAsync();
                    if (_options.LogRequestBodyAsStructuredObject && !string.IsNullOrWhiteSpace(requestBodyText))
                    {
                        JToken token;
                        if (requestBodyText.TryGetJToken(out token))
                        {
                            var jToken = token.MaskFields(_options.MaskedProperties.ToArray(),
                                _options.MaskFormat);
                            requestBodyText = jToken.ToString();
                            requestBody = jToken;
                        }

                        if (requestBodyText.Length > _options.RequestBodyLogTextLengthLimit)
                            requestBodyText = requestBodyText.Substring(0, _options.RequestBodyLogTextLengthLimit);
                    }
                }
                else
                {
                    requestBodyText = "(Not Logged)";
                }

                var requestHeaders = new Dictionary<string, object>();
                if (_options.RequestHeaderLogMode == LogMode.LogAll ||
                    (!isRequestOk && _options.RequestHeaderLogMode == LogMode.LogFailures))
                {
                    try
                    {
                        var valuesByKey = req.Headers.GetEnumerator()
                            .Mask(_options.MaskedProperties.ToArray(), _options.MaskFormat);
                        foreach (var item in valuesByKey)
                        {
                            if (item.Value.Count() > 1)
                                requestHeaders.Add(item.Key, item.Value);
                            else
                                requestHeaders.Add(item.Key, item.Value.First());
                        }
                    }
                    catch (Exception headerParseException)
                    {
                        SelfLog.WriteLine("Cannot parse request header: " + headerParseException);
                    }
                }

                var requestQuery = new Dictionary<string, object>();
                try
                {
                    if (!string.IsNullOrWhiteSpace(req.RequestUri.Query))
                    {
                        var q = HttpUtility.ParseQueryString(req.RequestUri.Query);

                        foreach (var key in q.AllKeys)
                        {
                            requestQuery.Add(key, q[key]);
                        }
                    }
                }
                catch (Exception)
                {
                    SelfLog.WriteLine("Cannot parse query string");
                }

                var requestData = new HttpRequestInfo
                {
                    Method = req.Method?.Method,
                    Scheme = req.RequestUri.Scheme, 
                    Host = req.RequestUri.Host,
                    Path = req.RequestUri.AbsolutePath,
                    QueryString = req.RequestUri.Query,
                    Query = requestQuery,
                    BodyString = requestBodyText,
                    Body = requestBody,
                    Headers = requestHeaders
                };

                object responseBody = null;
                if ((_options.ResponseBodyLogMode == LogMode.LogAll ||
                     (!isRequestOk && _options.ResponseBodyLogMode == LogMode.LogFailures)))
                {
                    if (resp?.Content != null)
                        responseBodyText = await resp?.Content.ReadAsStringAsync();
                    if (_options.LogResponseBodyAsStructuredObject && !string.IsNullOrWhiteSpace(responseBodyText))
                    {
                        JToken jToken;
                        if (responseBodyText.TryGetJToken(out jToken))
                        {
                            jToken = jToken.MaskFields(_options.MaskedProperties.ToArray(), _options.MaskFormat);
                            responseBodyText = jToken.ToString();
                            responseBody = jToken;
                        }

                        if (responseBodyText.Length > _options.ResponseBodyLogTextLengthLimit)
                            responseBodyText = responseBodyText.Substring(0, _options.ResponseBodyLogTextLengthLimit);
                    }
                }
                else
                {
                    responseBodyText = "(Not Logged)";
                }

                var responseHeaders = new Dictionary<string, object>();
                if (_options.ResponseHeaderLogMode == LogMode.LogAll ||
                    (!isRequestOk && _options.ResponseHeaderLogMode == LogMode.LogFailures)
                    && resp != null)
                {
                    try
                    {
                        var valuesByKey = resp.Headers.GetEnumerator()
                            .Mask(_options.MaskedProperties.ToArray(), _options.MaskFormat);
                        foreach (var item in valuesByKey)
                        {
                            if (item.Value.Count() > 1)
                                responseHeaders.Add(item.Key, item.Value);
                            else
                                responseHeaders.Add(item.Key, item.Value.First());
                        }
                    }
                    catch (Exception headerParseException)
                    {
                        SelfLog.WriteLine("Cannot parse response header: " + headerParseException);
                    }
                }

                var responseData = new HttpResponseInfo
                {
                    StatusCode = (int?)resp?.StatusCode, 
                    IsSucceed = isRequestOk, 
                    ElapsedMilliseconds = elapsedMs,
                    BodyString = responseBodyText,
                    Body = responseBody,
                    Headers = responseHeaders
                };
                
                var httpClientContext = new HttpClientContext { Request = requestData, Response = responseData };
                var messageOptions = _options.GetLogMessageAndProperties(httpClientContext);
                var contextLogger = _logger;
                if (messageOptions.AdditionalProperties != null)
                {
                    foreach (var p in messageOptions.AdditionalProperties)
                    {
                        contextLogger = contextLogger.ForContext(p.Key, p.Value, true);
                    }
                }
                contextLogger.Write(level, ex, messageOptions.MessageTemplate, messageOptions.MessageParameters);
            }
        }
    }
}
