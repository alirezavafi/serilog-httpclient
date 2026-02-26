using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog.Formatting.Json;
using Serilog.HttpClient.Extensions;
using Serilog.Sinks.SystemConsole.Themes;

namespace Serilog.HttpClient.Samples.ConsoleApp 
{
    class Program
    {
        static void Main(string[] args)
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.File(new JsonFormatter(),$"log-{DateTime.Now:yyyyMMdd-HHmmss}.json")
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} {NewLine}{Properties} {NewLine}{Exception}{NewLine}",
                    theme: SystemConsoleTheme.Literate)
                .AddJsonDestructuringPolicies()
                .Enrich.FromLogContext()
                .CreateLogger();

            var loggingHandler = new LoggingDelegatingHandler(new RequestLoggingOptions()
            {
                LogMode = LogMode.LogAll,
                RequestHeaderLogMode = LogMode.LogAll,
                RequestBodyLogMode = LogMode.LogAll,
                RequestBodyLogTextLengthLimit = 5000,
                ResponseHeaderLogMode = LogMode.LogAll,
                ResponseBodyLogMode = LogMode.LogAll,
                ResponseBodyLogTextLengthLimit = 5000,
                MaskFormat = "*****",
                MaskedProperties = {  "password", "token" },
            });

            var c = new System.Net.Http.HttpClient(loggingHandler);
            var o = Task.Run(() => c.GetFromJsonAsync<object>("https://jsonplaceholder.typicode.com/users/1")).Result;
        }
    }
}