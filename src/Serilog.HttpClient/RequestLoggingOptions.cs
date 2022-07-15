using System;
using System.Collections.Generic;
using System.Net.Http;
using Serilog.Events;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Serilog.HttpClient
{
    /// <summary>
    /// Contains options for the <see cref="LoggingDelegatingHandler"/>.
    /// </summary>
    public class RequestLoggingOptions
    {
        const string DefaultRequestCompletionMessageTemplate =
            "HTTP {RequestMethod} {RequestUri} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms";

        /// <summary>
        /// Gets or sets the message template. The default value is
        /// <c>"HTTP {RequestMethod} {RequestUri} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms"</c>.
        /// </summary>
        /// <value>
        /// The message template.
        /// </value>
        public string MessageTemplate { get; set; }

        /// <summary>
        /// A function returning the <see cref="LogEventLevel"/> based on the <see cref="HttpRequestMessage"/>/<see cref="HttpResponseMessage"/> information,
        /// the number of elapsed milliseconds required for handling the request, and an <see cref="Exception" /> if one was thrown.
        /// The default behavior returns <see cref="LogEventLevel.Error"/> when the response status code is greater than 499 or if the
        /// <see cref="Exception"/> is not null. Also default log level for 4xx range errors set to <see cref="LogEventLevel.Warning"/>
        /// </summary>
        /// <value>
        /// A function returning the <see cref="LogEventLevel"/>.
        /// </value>
        public Func<HttpRequestMessage, HttpResponseMessage, double, Exception, LogEventLevel> GetLevel { get; set; }

        /// <summary>
        /// The logger through which request completion events will be logged. The default is to use the
        /// static <see cref="Log"/> class.
        /// </summary>
        public ILogger Logger { get; set; }
        /// <summary>
        /// Determines when logging requests information. Default is true.
        /// </summary>
        public LogMode LogMode { get; set; } = LogMode.LogAll;

        /// <summary>
        /// Determines when logging request headers
        /// </summary>
        public LogMode RequestHeaderLogMode { get; set; } = LogMode.LogAll;
        /// <summary>
        /// Determines when logging request body data
        /// </summary>
        public LogMode RequestBodyLogMode { get; set; } = LogMode.LogAll;
        /// <summary>
        /// Determines when logging response headers
        /// </summary>
        public LogMode ResponseHeaderLogMode { get; set; } = LogMode.LogAll;
        /// <summary>
        /// Determines when logging response body data
        /// </summary>
        public LogMode ResponseBodyLogMode { get; set; } = LogMode.LogFailures;
        /// <summary>
        /// Properties to mask before logging to output to prevent sensitive data leakage
        /// </summary>
        public IList<string> MaskedProperties { get; } =
            new List<string>() { "*password*", "*token*", "*clientsecret*", "*bearer*", "*authorization*", "*client-secret*", "*otp" };
        /// <summary>
        /// Mask format to replace with masked data
        /// </summary>
        public string MaskFormat { get; set; } = "*** MASKED ***";
        /// <summary>
        /// Maximum allowed length of response body text to capture in logs
        /// </summary>
        public int ResponseBodyLogTextLengthLimit { get; set; } = 4000;
        /// <summary>
        /// Maximum allowed length of request body text to capture in logs
        /// </summary>
        public int RequestBodyLogTextLengthLimit { get; set; } = 4000;

        /// <summary>
        /// Constructor
        /// </summary>
        public RequestLoggingOptions()
        {
            MessageTemplate = DefaultRequestCompletionMessageTemplate;
            GetLevel = DefaultGetLevel;
        }

        static LogEventLevel DefaultGetLevel(HttpRequestMessage req, HttpResponseMessage resp, double elapsedMs, Exception ex)
        {
            var level = LogEventLevel.Information;
            if (ex != null || resp == null)
                level = LogEventLevel.Error;
            else if ((int)resp.StatusCode >= 500)
                level = LogEventLevel.Error;
            else if ((int)resp.StatusCode >= 400)
                level = LogEventLevel.Warning;

            return level;
        }
    }
}
