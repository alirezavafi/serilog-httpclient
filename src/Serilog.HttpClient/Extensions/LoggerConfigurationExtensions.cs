using Serilog.HttpClient.DestructuringPolicies;

namespace Serilog.HttpClient.Extensions
{
    public static class LoggerConfigurationExtensions
    {
        public static LoggerConfiguration AddJsonDestructuringPolicies(this LoggerConfiguration loggerConfiguration)
        {
            loggerConfiguration
                .Destructure.With<JsonNetDestructuringPolicy>()
                .Destructure.With<JsonDocumentDestructuringPolicy>();

            return loggerConfiguration;
        }
    }
}