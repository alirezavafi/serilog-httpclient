using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Serilog.HttpClient.Extensions
{
    public static class HttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds services required for logging request/response to each outgoing <see cref="HttpClient"/> request.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> to add the services to.</param>
        /// <param name="configureOptions">The action used to configure <see cref="RequestLoggingOptions"/>.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.</returns>
        public static IHttpClientBuilder LogRequestResponse(this IHttpClientBuilder builder,
            Action<RequestLoggingOptions> configureOptions = null)
        {
            if (configureOptions == null)
                configureOptions = options => { };
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));
            
            builder.Services.Configure(builder.Name, configureOptions);
            builder.Services.TryAddTransient(s =>
            {
                var o = s.GetRequiredService<IOptionsSnapshot<RequestLoggingOptions>>();
                return new LoggingDelegatingHandler(o.Get(builder.Name), default, true);
            });
            builder.AddHttpMessageHandler(s => s.GetRequiredService<LoggingDelegatingHandler>());

            return builder;
        }
    }
}