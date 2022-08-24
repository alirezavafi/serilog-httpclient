using System.Collections.Generic;
using System.Net.Http;

namespace Serilog.HttpClient.Models
{
    /// <summary>
    /// HTTP request information
    /// </summary>
    public class HttpRequestInfo
    {
        /// <summary>
        /// HTTP method like GET, POST, ...
        /// </summary>
        public string Method { get; set; }
        
        /// <summary>
        /// HTTP request scheme, HTTP or HTTPS
        /// </summary>
        public string Scheme { get; set; }
        
        /// <summary>
        /// Host name like www.example.com
        /// </summary>
        public string Host { get; set; }
        
        /// <summary>
        /// Request path. for example /api/v1/tickets
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Request query string. for example trackId=123514325&amp;page=10
        /// </summary>
        public string QueryString { get; set; }
        
        /// <summary>
        /// Query string as dictionary object for structured logging and searching on platforms like elastic, splunk ...
        /// </summary>
        public Dictionary<string, object> Query { get; set; }
        
        /// <summary>
        /// Request body as string. request body maybe trimmed if exceeds length limit as specified in request logging option 
        /// </summary>
        public string BodyString { get; set; }
        
        /// <summary>
        /// Request body as object for structured logging and searching on platforms like elastic, splunk. this property populated when enabled on request logging option (LogRequestBodyAsStructuredObject). default is true.
        /// </summary>
        public object Body { get; set; }
        
        /// <summary>
        /// Request headers. masking also applied on request headers.
        /// </summary>
        public Dictionary<string, object> Headers { get; set; }
    }
}