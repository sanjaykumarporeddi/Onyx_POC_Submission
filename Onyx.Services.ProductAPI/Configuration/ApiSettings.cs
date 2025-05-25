using System.ComponentModel.DataAnnotations;

namespace Onyx.Services.ProductAPI.Configuration
{
    public class ApiSettings
    {
        public const string SectionName = "ApiSettings";

        [Required(AllowEmptyStrings = false, ErrorMessage = "JWT Secret is required.")]
        [MinLength(32, ErrorMessage = "JWT Secret must be at least 32 characters (256 bits) long for HMACSHA256 security.")]
        public string Secret { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "JWT Issuer is required.")]
        public string Issuer { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "JWT Audience is required.")]
        public string Audience { get; set; } = string.Empty;
    }
}