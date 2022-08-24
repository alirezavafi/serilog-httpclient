namespace Serilog.HttpClient.Models
{
    /// <summary>
    /// HTTP client request/response contextual properties
    /// </summary>
    public class HttpClientContext
    {
        /// <summary>
        /// HTTP request information
        /// </summary>
        public HttpRequestInfo Request { get; set; }
        
        /// <summary>
        /// HTTP response information
        /// </summary>
        public HttpResponseInfo Response { get; set; }
    }
}