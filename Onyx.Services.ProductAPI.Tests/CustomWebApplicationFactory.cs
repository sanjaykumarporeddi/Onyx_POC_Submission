using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onyx.Services.ProductAPI.Data;
using System;
using System.Linq;
using Onyx.Services.ProductAPI;
using Onyx.Services.ProductAPI.Common;

namespace Onyx.Services.ProductAPI.Tests
{
    /// <summary>
    /// Custom WAF for integration tests: in-memory DB, test environment.
    /// </summary>
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        private readonly string _dbName = $"InMemoryDbForTesting_{Guid.NewGuid()}";

        public CustomWebApplicationFactory()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", AppConstants.DevelopmentEnvironment);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Program.cs handles Serilog setup.
            var host = base.CreateHost(builder);

            // Initialize DB after host is built.
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var dbContext = services.GetRequiredService<AppDbContext>();
                var logger = services.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();
                try
                {
                    dbContext.Database.EnsureCreated(); // Applies OnModelCreating().HasData().
                    logger.LogInformation("In-memory database {DbName} ensured created for tests via CreateHost.", _dbName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while ensuring the database was created for {DbName} in CreateHost.", _dbName);
                    throw;
                }
            }
            return host;
        }

        protected override void ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.UseEnvironment(AppConstants.DevelopmentEnvironment);
            webHostBuilder.ConfigureServices(services =>
            {
                // Remove original DBContext options.
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);
                var dbContextService = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
                if (dbContextService != null) services.Remove(dbContextService);

                // Add InMemory DBContext.
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            });
        }
    }
}