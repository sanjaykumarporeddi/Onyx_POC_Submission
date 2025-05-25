using Onyx.Services.ProductAPI.Extensions;
using Serilog;
using Microsoft.Extensions.Options;
using Onyx.Services.ProductAPI.Common;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Onyx.Services.ProductAPI
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    public partial class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Pre-Host Boot: Configuring {AppConstants.ApplicationName} host...");

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Configure Serilog first for robust logging.
                builder.ConfigureSerilog();

                builder.Services.AddApiConfiguration(builder.Configuration);
                builder.Services.AddDatabaseServices(builder.Configuration);
                builder.Services.AddCoreApplicationServices();
                builder.Services.AddPresentationLayerServices(builder.Configuration, builder.Environment);
                builder.AddAppAuthentication();
                builder.Services.AddAuthorization();

                var app = builder.Build();

                app.ConfigureMiddlewarePipeline();

                Log.Information(AppConstants.LogMessages.AppStartingUp);
                app.Run();
            }
            catch (OptionsValidationException ex)
            {
                string joinedFailures = string.Join("; ", ex.Failures);
                Console.Error.WriteLine($"CRITICAL CONFIG ERROR: {string.Format(AppConstants.ExceptionMessages.CriticalConfigValidationFailedFormat, joinedFailures)}");
                Log.Fatal(ex, AppConstants.ExceptionMessages.CriticalConfigValidationFailedFormat, joinedFailures);
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CRITICAL HOST ERROR: {AppConstants.ExceptionMessages.CriticalHostTerminatedUnexpectedly} - {ex.Message}");
                Log.Fatal(ex, AppConstants.ExceptionMessages.CriticalHostTerminatedUnexpectedly);
                throw;
            }
            finally
            {
                Log.Information(AppConstants.LogMessages.AppShuttingDown);
                Log.CloseAndFlush(); // Ensure all logs are flushed.
            }
        }
    }
}