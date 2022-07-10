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
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.HttpClient
{
    public class LoggingDelegatingHandler : DelegatingHandler
    {
        private readonly RequestLoggingOptions _options;
        private readonly ILogger _logger;
        private readonly MessageTemplate _messageTemplate;

        public LoggingDelegatingHandler(
            RequestLoggingOptions options,
            HttpMessageHandler httpMessageHandler = default)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = options.Logger?.ForContext<LoggingDelegatingHandler>() ?? Serilog.Log.Logger.ForContext<LoggingDelegatingHandler>();
            _messageTemplate = new MessageTemplateParser().Parse(options.MessageTemplate);
            InnerHandler = httpMessageHandler ?? new HttpClientHandler();
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

            var properties = new List<LogEventProperty>();

            var requestBodyText = string.Empty;
            var responseBodyText = string.Empty;

            var isRequestOk = !(resp != null && (int)resp.StatusCode >= 400 || ex != null);
            if (_options.LogMode == LogMode.LogAll ||
                (!isRequestOk && _options.LogMode == LogMode.LogFailures))
            {
                if (req.Content != null)
                    requestBodyText = await req.Content.ReadAsStringAsync();
                JsonDocument requestBody = null;
                if ((_options.RequestBodyLogMode == LogMode.LogAll ||
                     (!isRequestOk && _options.RequestBodyLogMode == LogMode.LogFailures)))
                {
                    if (!string.IsNullOrWhiteSpace(requestBodyText))
                    {
                        try
                        {
                            requestBodyText = requestBodyText.MaskFields(_options.MaskedProperties.ToArray(),
                                _options.MaskFormat);
                        }
                        catch (Exception) { }

                        if (requestBodyText.Length > _options.RequestBodyLogTextLengthLimit)
                            requestBodyText = requestBodyText.Substring(0, _options.RequestBodyLogTextLengthLimit);
                        else
                            try { requestBody = System.Text.Json.JsonDocument.Parse(requestBodyText); } catch (Exception) { }
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
                        var valuesByKey = req.Headers
                            .Mask(_options.MaskedProperties.ToArray(), _options.MaskFormat).GroupBy(x => x.Key);
                        foreach (var item in valuesByKey)
                        {
                            if (item.Count() > 1)
                                requestHeaders.Add(item.Key, item.SelectMany(x => x.Value));
                            else
                                requestHeaders.Add(item.Key, item.First().Value);
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

                properties.Add(new LogEventProperty("RequestMethod", new ScalarValue(req.Method.Method)));
                properties.Add(new LogEventProperty("RequestScheme", new ScalarValue(req.RequestUri.Scheme)));
                properties.Add(new LogEventProperty("RequestHost", new ScalarValue(req.RequestUri.Host)));
                properties.Add(new LogEventProperty("RequestPath", new ScalarValue(req.RequestUri.AbsolutePath)));
                properties.Add(new LogEventProperty("RequestQueryString", new ScalarValue(req.RequestUri.Query)));
                properties.Add(new LogEventProperty("RequestQuery", new ScalarValue(requestQuery)));
                properties.Add(new LogEventProperty("RequestBodyString", new ScalarValue(requestBodyText)));
                properties.Add(new LogEventProperty("RequestBody", new ScalarValue(requestBody)));
                properties.Add(new LogEventProperty("RequestHeaders", new ScalarValue(requestHeaders)));

                object responseBody = null;
                if ((_options.ResponseBodyLogMode == LogMode.LogAll ||
                     (!isRequestOk && _options.ResponseBodyLogMode == LogMode.LogFailures)))
                {
                    if (resp?.Content != null)
                        responseBodyText = await resp?.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(responseBodyText))
                    {
                        try
                        {
                            responseBodyText = responseBodyText.MaskFields(_options.MaskedProperties.ToArray(),
                                _options.MaskFormat);
                        }
                        catch (Exception) { }

                        if (responseBodyText.Length > _options.ResponseBodyLogTextLengthLimit)
                            responseBodyText = responseBodyText.Substring(0, _options.ResponseBodyLogTextLengthLimit);
                        else
                            try { responseBody = System.Text.Json.JsonDocument.Parse(responseBodyText); } catch (Exception) { }
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
                        var valuesByKey = resp.Headers
                            .Mask(_options.MaskedProperties.ToArray(), _options.MaskFormat).GroupBy(x => x.Key);
                        foreach (var item in valuesByKey)
                        {
                            if (item.Count() > 1)
                                responseHeaders.Add(item.Key, item.SelectMany(x => x.Value));
                            else
                                responseHeaders.Add(item.Key, item.First().Value);
                        }
                    }
                    catch (Exception headerParseException)
                    {
                        SelfLog.WriteLine("Cannot parse response header: " + headerParseException);
                    }
                }

                properties.Add(new LogEventProperty("StatusCode", new ScalarValue(resp?.StatusCode.ToString())));
                properties.Add(new LogEventProperty("IsSucceed", new ScalarValue(isRequestOk)));
                properties.Add(new LogEventProperty("ElapsedMilliseconds", new ScalarValue(elapsedMs)));
                properties.Add(new LogEventProperty("ResponseBodyString", new ScalarValue(responseBodyText)));
                properties.Add(new LogEventProperty("ResponseBody", new ScalarValue(responseBody)));
                properties.Add(new LogEventProperty("ResponseHeaders", new ScalarValue(responseHeaders)));

                var evt = new LogEvent(DateTimeOffset.Now, level, ex, _messageTemplate, properties);
                _logger.Write(evt);
            }
        }
    }
}
