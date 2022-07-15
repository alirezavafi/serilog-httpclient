using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Correlate.AspNetCore;
using Correlate.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.HttpClient.Extensions;
using Serilog.HttpClient.Samples.AspNetCore.Services;

namespace Serilog.HttpClient.Samples.AspNetCore
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCorrelate(options => options.RequestHeaders = new [] { "X-Correlation-ID" });

            services
                .AddHttpClient<IMyService, MyService>()
                .CorrelateRequests("X-Correlation-ID")
                .LogRequestResponse(p =>
                {
                    p.LogMode = LogMode.LogAll;
                    p.RequestHeaderLogMode = LogMode.LogAll;
                    p.RequestBodyLogMode = LogMode.LogAll;
                    p.RequestBodyLogTextLengthLimit = 5000;
                    p.ResponseHeaderLogMode = LogMode.LogAll;
                    p.ResponseBodyLogMode = LogMode.LogAll;
                    p.ResponseBodyLogTextLengthLimit = 5000;
                    p.MaskFormat = "*****"; 
                    p.MaskedProperties.Clear();
                    p.MaskedProperties.Add("*password*");
                    p.MaskedProperties.Add("*token*");
                })
                // /*OR*/ .LogRequestResponse()
                .ConfigurePrimaryHttpMessageHandler(p => new HttpClientHandler()
                {
                    //Proxy = new WebProxy("127.0.0.1", 8888)
                });
            //or
 
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCorrelate();
            app.UseSerilogPlusRequestLogging();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}