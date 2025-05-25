using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Onyx.Services.ProductAPI.Common; // For AppConstants.ProblemDetails

namespace Onyx.Services.ProductAPI.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetailsService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {ErrorMessage}", ex.Message);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                if (problemDetailsService != null)
                {
                    await problemDetailsService.WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context,
                        ProblemDetails =
                        {
                            Status = StatusCodes.Status500InternalServerError,
                            Title = AppConstants.ProblemDetails.Titles.GenericError,
                            Detail = _env.IsDevelopment() ? ex.ToString() : AppConstants.ProblemDetails.Titles.GenericError + ". Please try again later.",
                            Instance = context.Request.Path
                        }
                    });
                }
                else
                {
                    context.Response.ContentType = AppConstants.ContentTypes.ApplicationJson;
                    var fallbackResponse = JsonSerializer.Serialize(new
                    {
                        title = AppConstants.ProblemDetails.Titles.GenericError,
                        status = StatusCodes.Status500InternalServerError,
                        detail = _env.IsDevelopment() ? ex.ToString() : AppConstants.ProblemDetails.Titles.GenericError + ". Please try again later."
                    });
                    await context.Response.WriteAsync(fallbackResponse);
                }
            }
        }
    }

    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}