using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Onyx.Services.ProductAPI.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Onyx.Services.ProductAPI.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Onyx.Services.ProductAPI.Extensions
{
    /// <summary>
    /// WebApplicationBuilder extensions for authentication.
    /// </summary>
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder AddAppAuthentication(this WebApplicationBuilder builder)
        {
            var apiSettings = new ApiSettings();
            builder.Configuration.GetSection(ApiSettings.SectionName).Bind(apiSettings);

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(apiSettings, serviceProvider: null, items: null);
            bool isValid = Validator.TryValidateObject(apiSettings, validationContext, validationResults, validateAllProperties: true);

            if (!isValid)
            {
                var errorMessages = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
                Console.Error.WriteLine(string.Format(AppConstants.ExceptionMessages.CriticalErrorJwtSettingsInvalidConsoleFormat, errorMessages, ApiSettings.SectionName));
                throw new InvalidOperationException(string.Format(AppConstants.ExceptionMessages.JwtSettingsInvalidFormat, errorMessages));
            }

            var key = Encoding.UTF8.GetBytes(apiSettings.Secret);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = apiSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = apiSettings.Audience,
                    ClockSkew = TimeSpan.Zero
                };
            });

            return builder;
        }
    }
}