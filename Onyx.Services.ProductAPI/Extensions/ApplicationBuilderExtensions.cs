using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Onyx.Services.ProductAPI.Configuration;
using Onyx.Services.ProductAPI.Data;
using Onyx.Services.ProductAPI.Middleware;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Onyx.Services.ProductAPI.Common;
using Onyx.Common.Shared.Enums;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace Onyx.Services.ProductAPI.Extensions
{
    /// <summary>
    /// Configures the HTTP request pipeline.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"]);
                    var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
                    if (remoteIpAddress != null) { diagnosticContext.Set("RemoteIpAddress", remoteIpAddress.ToString()); }
                };
            });

            app.UseResponseCompression();
            app.UseGlobalExceptionHandler();
            app.UseSecurityHeaders();
            app.UseRateLimiter();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint(
                    string.Format(AppConstants.Swagger.EndpointPathFormat, AppConstants.Swagger.VersionV1),
                    $"{AppConstants.Swagger.ApiTitle} {AppConstants.Swagger.VersionV1}"));
                app.ApplyMigrationsAndSeedData(); // Apply DB migrations/seed in dev.
            }
            else
            {
                app.UseHsts();
            }

            app.UseStatusCodePages();
            app.UseHttpsRedirection();
            app.UseCors(AppConstants.CorsPolicies.AllowSpecificOrigins);
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers().RequireAuthorization();

            if (app.Environment.IsDevelopment())
            {
                app.MapDevelopmentEndpoints(); // Development-specific endpoints.
            }

            // Anonymous health check endpoint.
            app.MapGet(AppConstants.ApiRoutes.Health, () => Results.Text("Healthy", AppConstants.ContentTypes.PlainTextUtf8))
                .RequireRateLimiting(AppConstants.RateLimitPolicies.FixedRead).AllowAnonymous();

            return app;
        }

        /// <summary>
        /// Applies DB migrations/seeds data (for development).
        /// </summary>
        private static void ApplyMigrationsAndSeedData(this WebApplication webApp)
        {
            using var scope = webApp.Services.CreateScope();
            var services = scope.ServiceProvider;
            var dbContext = services.GetRequiredService<AppDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            try
            {
                logger.LogInformation(AppConstants.LogMessages.ApplyMigrations);
                if (dbContext.Database.IsRelational())
                {
                    logger.LogInformation(AppConstants.LogMessages.RelationalDbDetected);
                    if (dbContext.Database.GetPendingMigrations().Any())
                    {
                        logger.LogInformation(AppConstants.LogMessages.ApplyingMigrations);
                        dbContext.Database.Migrate();
                        logger.LogInformation(AppConstants.LogMessages.MigrationsApplied);
                    }
                    else
                    {
                        logger.LogInformation(AppConstants.LogMessages.NoPendingMigrations);
                    }
                }
                else
                {
                    logger.LogInformation(AppConstants.LogMessages.NonRelationalDbDetected);
                    dbContext.Database.EnsureCreated(); // Applies OnModelCreating().HasData() for InMemory.
                    logger.LogInformation(AppConstants.LogMessages.DbEnsuredCreated);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, AppConstants.LogMessages.DbSetupError);
                throw;
            }
        }

        /// <summary>
        /// Maps development-only endpoints (e.g., test token generation).
        /// </summary>
        private static void MapDevelopmentEndpoints(this WebApplication app)
        {
            app.MapGet(AppConstants.ApiRoutes.IntegrationTestTokenAdmin,
                (IOptions<ApiSettings> apiSettingsOptions, ILogger<Program> logger) =>
                {
                    var apiSettings = apiSettingsOptions.Value;
                    if (string.IsNullOrEmpty(apiSettings.Secret) || apiSettings.Secret.Length < 32)
                    {
                        var partialSecret = apiSettings.Secret != null && apiSettings.Secret.Length > 5 ? apiSettings.Secret.Substring(0, 5) + "..." : "(empty or too short)";
                        logger.LogCritical(AppConstants.ExceptionMessages.CriticalErrorJwtSecretForTestTokenInvalidFormat, partialSecret);
                        throw new InvalidOperationException(AppConstants.ExceptionMessages.TestTokenJwtSecretInvalid);
                    }

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiSettings.Secret));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, "testadminuser_integration"),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.NameIdentifier, "testadmin_id"),
                        new Claim(ClaimTypes.Name, "Test Admin User (Integration)"),
                        new Claim(ClaimTypes.Role, nameof(ApplicationRole.Admin))
                    };

                    var token = new JwtSecurityToken(
                        issuer: apiSettings.Issuer,
                        audience: apiSettings.Audience,
                        claims: claims,
                        expires: DateTime.UtcNow.AddHours(1),
                        signingCredentials: creds
                    );

                    logger.LogInformation(AppConstants.LogMessages.GeneratedTestToken, apiSettings.Issuer, apiSettings.Audience);
                    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
                })
                .RequireRateLimiting(AppConstants.RateLimitPolicies.TestTokenGeneration)
                .AllowAnonymous()
                .ExcludeFromDescription(); // Hide from Swagger.
        }
    }
}