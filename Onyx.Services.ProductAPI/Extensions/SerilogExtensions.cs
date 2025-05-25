using Serilog;
using Onyx.Services.ProductAPI.Common;
using Microsoft.AspNetCore.Builder;

namespace Onyx.Services.ProductAPI.Extensions
{
    /// <summary>
    /// Serilog configuration extensions.
    /// </summary>
    public static class SerilogExtensions
    {
        public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", AppConstants.ApplicationName)
            );
            return builder;
        }
    }
}