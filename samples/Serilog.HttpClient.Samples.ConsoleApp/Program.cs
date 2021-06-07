using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog.Formatting.Json;

namespace Serilog.HttpClient.Samples.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.File(new JsonFormatter(),"log.json")
                .CreateLogger();

            var loggingHandler = new LoggingDelegatingHandler(new RequestLoggingOptions()
            {
                LogMode = LogMode.LogAll,
                RequestHeaderLogMode = LogMode.LogAll,
                RequestBodyLogMode = LogMode.LogAll,
                RequestBodyLogTextLengthLimit = 5000,
                ResponseHeaderLogMode = LogMode.LogFailures,
                ResponseBodyLogMode = LogMode.LogFailures,
                ResponseBodyLogTextLengthLimit = 5000,
                MaskFormat = "*****",
                MaskedProperties = {  "password", "token" },
            });

            var c = new System.Net.Http.HttpClient(loggingHandler);
            var o = Task.Run(() => c.GetFromJsonAsync<object>("https://reqres.in/api/users?page=2")).Result;
        }
    }
}