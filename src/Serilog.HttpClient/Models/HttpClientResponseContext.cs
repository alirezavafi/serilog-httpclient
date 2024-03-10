using System.Collections.Generic;

namespace Serilog.HttpClient.Models
{
    /// <summary>
    /// HTTP response information
    /// </summary>
    public class HttpClientResponseContext
    {
        /// <summary>
        /// HTTP response status code
        /// </summary>
        public int? StatusCode { get; set; }
        
        /// <summary>
        /// Determines whether http request succeed or not. HTTP request determined as succeed if http status code less than 400 and no exception has been occured
        /// </summary>
        public bool IsSucceed { get; set; }
        
        /// <summary>
        /// Time separated that request have been executed
        /// </summary>
        public double ElapsedMilliseconds { get; set; }
        
        /// <summary>
        /// Response body as string. response body maybe trimmed if exceeds length limit as specified in request logging option 
        /// </summary>
        public string BodyString { get; set; }
        
        /// <summary>
        /// Response body as object for structured logging and searching on platforms like elastic, splunk. this property populated when enabled on request logging option (LogResponseBodyAsStructuredObject). default is true.
        /// </summary>
        public object Body { get; set; }
        
        /// <summary>
        /// Request headers. masking also applied on request headers.
        /// </summary>
        public Dictionary<string, object> Headers { get; set; }
    }
}