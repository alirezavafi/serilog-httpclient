namespace Serilog.HttpClient
{
    /// <summary>
    /// Determines when do logging
    /// </summary>
    public enum LogMode
    {
        /// <summary>
        /// Logs no data whether operation succeed or failed
        /// </summary>
        LogNone,
        
        /// <summary>
        /// Logs all http requests including success and failures
        /// </summary>
        LogAll,
        
        /// <summary>
        /// Log only failed http requests
        /// </summary>
        LogFailures
    }
}