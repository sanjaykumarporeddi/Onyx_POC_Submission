using AutoMapper;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Onyx.Services.ProductAPI.Configuration;
using Onyx.Services.ProductAPI.Data;
using Onyx.Services.ProductAPI.Repository;
using Onyx.Services.ProductAPI.Common;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.Extensions.Caching.Memory;
using Onyx.Services.ProductAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using Onyx.MessageBus;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Threading.RateLimiting;

namespace Onyx.Services.ProductAPI.Extensions
{
    /// <summary>
    /// IServiceCollection extension methods for DI setup.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ApiSettings>()
                .Bind(configuration.GetSection(ApiSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            return services;
        }

        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString(AppConstants.ConfigSections.DefaultConnection)));
            services.AddScoped<IProductRepository, ProductRepository>();
            return services;
        }

        public static IServiceCollection AddCoreApplicationServices(this IServiceCollection services)
        {
            IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
            services.AddSingleton(mapper);
            services.AddMemoryCache();
            services.AddSingleton<IMessageBus, AzureMessageBus>();
            services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();
            // services.AddSingleton<IEventPublisher, LoggingEventPublisher>(); // Alternative for local dev
            return services;
        }

        public static IServiceCollection AddPresentationLayerServices(
            this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            services.AddControllers().AddNewtonsoftJson(); // Newtonsoft for JsonPatch
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = ctx =>
                {
                    if (string.IsNullOrEmpty(ctx.ProblemDetails.Instance))
                        ctx.ProblemDetails.Instance = ctx.HttpContext.Request.Path;
                    if (ctx.HttpContext?.TraceIdentifier != null)
                        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
                };
            });
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Fastest; });
            services.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Fastest; });

            ConfigureRateLimiting(services, environment);
            ConfigureCors(services, configuration, environment);
            ConfigureSwagger(services);
            return services;
        }

        private static void ConfigureRateLimiting(IServiceCollection services, IWebHostEnvironment environment)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddFixedWindowLimiter(policyName: AppConstants.RateLimitPolicies.FixedRead, fixedOptions =>
                {
                    fixedOptions.PermitLimit = 100;
                    fixedOptions.Window = TimeSpan.FromSeconds(10);
                    fixedOptions.QueueLimit = 5;
                    fixedOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
                options.AddFixedWindowLimiter(policyName: AppConstants.RateLimitPolicies.FixedWrite, fixedOptions =>
                {
                    fixedOptions.PermitLimit = 10;
                    fixedOptions.Window = TimeSpan.FromSeconds(10);
                    fixedOptions.QueueLimit = 2;
                    fixedOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
                if (environment.IsDevelopment())
                {
                    options.AddFixedWindowLimiter(policyName: AppConstants.RateLimitPolicies.TestTokenGeneration, fixedOptions =>
                    {
                        fixedOptions.PermitLimit = 50;
                        fixedOptions.Window = TimeSpan.FromMinutes(1);
                        fixedOptions.QueueLimit = 0;
                        fixedOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    });
                }
            });
        }
        private static void ConfigureCors(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(AppConstants.CorsPolicies.AllowSpecificOrigins, policyBuilder =>
                {
                    string[] allowedOrigins = configuration.GetSection(AppConstants.ConfigSections.CorsAllowedOrigins).Get<string[]>()
                                              ?? (environment.IsDevelopment()
                                                  ? new[] { "http://localhost:3000", "https://localhost:3001", "http://localhost:5173", "http://localhost:4200" }
                                                  : Array.Empty<string>());
                    if (allowedOrigins.Any())
                    {
                        policyBuilder.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                    }
                    else if (environment.IsProduction())
                    {
                        Console.WriteLine($"Warning: CORS policy '{AppConstants.CorsPolicies.AllowSpecificOrigins}' has NO origins configured for production.");
                    }
                });
            });
        }
        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc(AppConstants.Swagger.VersionV1, new OpenApiInfo { Title = AppConstants.Swagger.ApiTitle, Version = AppConstants.Swagger.VersionV1 });
                option.AddSecurityDefinition(AppConstants.Swagger.AuthScheme, new OpenApiSecurityScheme
                {
                    Name = AppConstants.HttpHeaders.Authorization,
                    Description = AppConstants.Swagger.AuthDescription,
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = AppConstants.Swagger.AuthScheme
                });
                option.AddSecurityRequirement(new OpenApiSecurityRequirement{{
                    new OpenApiSecurityScheme{Reference = new OpenApiReference{Type = ReferenceType.SecurityScheme,Id = AppConstants.Swagger.AuthScheme}},Array.Empty<string>()
                }});
            });
        }
    }
}