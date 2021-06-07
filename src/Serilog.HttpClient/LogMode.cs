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
        /// Logs all including success and failures
        /// </summary>
        LogAll,
        /// <summary>
        /// Log only failures
        /// </summary>
        LogFailures
    }
}