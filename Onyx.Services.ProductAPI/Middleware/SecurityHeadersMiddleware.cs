using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Onyx.Services.ProductAPI.Common; // Primary constants file

namespace Onyx.Services.ProductAPI.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers.Append(AppConstants.HttpHeaders.ContentTypeOptions, AppConstants.SecurityHeaderValues.NoSniff);
            context.Response.Headers.Append(AppConstants.HttpHeaders.FrameOptions, AppConstants.SecurityHeaderValues.Deny);
            if (!context.Response.Headers.ContainsKey(AppConstants.HttpHeaders.ContentSecurityPolicy))
            {
                context.Response.Headers.Append(AppConstants.HttpHeaders.ContentSecurityPolicy, AppConstants.SecurityHeaderValues.CspProductApi);
            }
            context.Response.Headers.Append(AppConstants.HttpHeaders.ReferrerPolicy, AppConstants.SecurityHeaderValues.StrictOriginWhenCrossOrigin);
            context.Response.Headers.Append(AppConstants.HttpHeaders.PermissionsPolicy, AppConstants.SecurityHeaderValues.PermissionsPolicyMinimal);
            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}