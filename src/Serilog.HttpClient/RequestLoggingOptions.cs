using System;
using System.Collections.Generic;
using System.Net.Http;
using Serilog.Events;
using Serilog.HttpClient;
using Serilog.HttpClient.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Serilog.HttpClient
{
    /// <summary>
    /// Contains options for the <see cref="LoggingDelegatingHandler"/>.
    /// </summary>
    public class RequestLoggingOptions
    {
        /// <summary>
        /// Gets or sets the message template. The default value is
        /// <c>"HTTP Client Request Completed {@Context}"</c>. The
        /// template can contain any of the placeholders from the default template, names of properties
        /// </summary>
        /// <value>
        /// The message template.
        /// </value>
        public string MessageTemplate { get; set; }
    
        /// <summary>
        /// A function to specify the values of the MessageTemplateProperties.
        /// </summary>
        public Func<HttpClientContext, ILogger, IEnumerable<LogEventProperty>> GetMessageTemplateProperties { get; set; }
        
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
        /// Determines weather to log request as structured object instead of string. This is useful when you use Elastic, Splunk or any other platform to search on object properties. Default is true. Masking only works when this options is enabled.
        /// </summary>
        public bool LogRequestBodyAsStructuredObject { get; set; } = true;
        
        /// <summary>
        /// Determines when logging response headers
        /// </summary>
        public LogMode ResponseHeaderLogMode { get; set; } = LogMode.LogAll;
        
        /// <summary>
        /// Determines when logging response body data
        /// </summary>
        public LogMode ResponseBodyLogMode { get; set; } = LogMode.LogFailures;
        
        /// <summary>
        /// Determines weather to log response as structured object instead of string. This is useful when you use Elastic, Splunk or any other platform to search on object properties. Default is true. Masking only works when this options is enabled.
        /// </summary>
        public bool LogResponseBodyAsStructuredObject { get; set; } = true;
        
        /// <summary>
        /// Properties to mask request/response body and headers before logging to output to prevent sensitive data leakage
        /// default is "*password*", "*token*", "*secret*", "*bearer*", "*authorization*","*otp"
        /// </summary>
        public IList<string> MaskedProperties { get; } = new List<string>() {"*password*", "*token*", "*secret*", "*bearer*", "*authorization*","*otp"};
        
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
            GetLevel = DefaultGetLevel;
            MessageTemplate = DefaultRequestCompletionMessageTemplate;
            GetMessageTemplateProperties = DefaultGetMessageTemplateProperties;
        }

        private static LogEventLevel DefaultGetLevel(HttpRequestMessage req, HttpResponseMessage resp, double elapsedMs, Exception ex)
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
        
        const string DefaultRequestCompletionMessageTemplate = "HTTP Client Request Completed {@Context}";

        static IEnumerable<LogEventProperty> DefaultGetMessageTemplateProperties(HttpClientContext httpContextInfo, ILogger logger)
        {
            logger.BindProperty("Context", httpContextInfo, true, out var prop);
            if (prop != null)
                return new[] { prop };

            return Array.Empty<LogEventProperty>();
        }

    }
}
