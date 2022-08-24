using System.Collections.Generic;

namespace Serilog.HttpClient
{
    /// <summary>
    /// Log entry output parameters
    /// </summary>
    public class LogEntryParameters
    {
        /// <summary>
        /// Message Template to log 
        /// </summary>
        public string MessageTemplate { get; set; }

        /// <summary>
        /// Message parameter values as specified on message template 
        /// </summary>
        public object[] MessageParameters { get; set; }
        
        /// <summary>
        /// Additional properties to enrich
        /// </summary>
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
    }
}