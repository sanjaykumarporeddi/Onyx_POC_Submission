namespace Onyx.Services.ProductAPI.Common
{
    public static class AppConstants
    {
        public const string ApplicationName = "Onyx.ProductAPI";
        public const string DevelopmentEnvironment = "Development";
        public const string ProductionEnvironment = "Production";

        public static class RateLimitPolicies
        {
            public const string FixedRead = "fixed_read_operations";
            public const string FixedWrite = "fixed_write_operations";
            public const string TestTokenGeneration = "test_token_generation";
        }

        public static class CorsPolicies
        {
            public const string AllowSpecificOrigins = "AllowSpecificOrigins";
        }

        public static class ConfigSections
        {
            public const string CorsAllowedOrigins = "CorsSettings:AllowedOrigins";
            public const string DefaultConnection = "DefaultConnection";
            public const string ApiSettings = Configuration.ApiSettings.SectionName;
            public const string ServiceBusTopics = "ServiceBusTopics";
        }

        public static class HttpHeaders
        {
            public const string Authorization = "Authorization"; // Standard
            public const string ContentTypeOptions = "X-Content-Type-Options";
            public const string FrameOptions = "X-Frame-Options";
            public const string ContentSecurityPolicy = "Content-Security-Policy";
            public const string ReferrerPolicy = "Referrer-Policy";
            public const string PermissionsPolicy = "Permissions-Policy";
            // ProductAPI specific headers (if any, XPagination was removed with PagedResultDto)
            public const string XPagination = "X-Pagination";
        }

        public static class ApiRoutes
        {
            public const string ProductsBase = "api/products";
            public const string Health = "/health";
            public const string IntegrationTestTokenAdmin = "/integrationtests/generatetoken/admin";
            public static class Names
            {
                public const string GetProductById = "GetProductById";
                public const string GetProductByName = "GetProductByName";
                public const string CreateProduct = "CreateProduct";
                public const string UpdateProduct = "UpdateProduct";
                public const string PartiallyUpdateProduct = "PartiallyUpdateProduct";
                public const string DeleteProduct = "DeleteProduct";
                public const string GetAllProducts = "GetAllProducts";
            }
        }

        public static class Swagger
        {
            public const string VersionV1 = "v1";
            public const string ApiTitle = "Onyx Product API";
            public const string AuthScheme = "Bearer";
            public const string AuthDescription = "Enter the Bearer token as: 'Bearer Generated-JWT-Token'";
            public const string EndpointPathFormat = "/swagger/{0}/swagger.json";
        }

        public static class CacheKeys
        {
            public const string ProductPrefix = "Product-";
        }

        public static class ContentTypes
        {
            public const string PlainTextUtf8 = "text/plain; charset=utf-8";
            public const string JsonPatch = "application/json-patch+json";
            public const string ApplicationJson = "application/json";
            public const string ApplicationProblemJson = "application/problem+json";
        }

        public static class SecurityHeaderValues
        {
            public const string NoSniff = "nosniff";
            public const string Deny = "DENY";
            public const string StrictOriginWhenCrossOrigin = "strict-origin-when-cross-origin";
            public const string PermissionsPolicyMinimal = "geolocation=(), microphone=(), camera=(), usb=(), payment=(), autoplay=()";
            public const string CspProductApi = CspDefaultSrcSelf + CspFrameAncestorsNone + CspFormActionSelf + CspObjectSrcNone + CspScriptSrcSelf + CspStyleSrcSelfUnsafeInline + CspImgSrcSelfData + CspFontSrcSelf;
        }
        private const string CspDefaultSrcSelf = "default-src 'self'; ";
        private const string CspFrameAncestorsNone = "frame-ancestors 'none'; ";
        private const string CspFormActionSelf = "form-action 'self'; ";
        private const string CspObjectSrcNone = "object-src 'none'; ";
        private const string CspScriptSrcSelf = "script-src 'self'; ";
        private const string CspStyleSrcSelfUnsafeInline = "style-src 'self' 'unsafe-inline'; ";
        private const string CspImgSrcSelfData = "img-src 'self' data:; ";
        private const string CspFontSrcSelf = "font-src 'self';";


        // Log Message Templates specific to ProductAPI
        public static class LogMessages
        {
            public const string AttemptingGetAllProducts = "Controller: Attempting to get all products. Query: {QueryParams}";
            public const string AttemptingGetProductById = "Controller: Attempting to get product by ID: {ProductId}";
            public const string AttemptingGetProductByName = "Controller: Attempting to get product by name: {ProductName}";
            public const string AttemptingCreateProduct = "Controller: Attempting to create product: {ProductName}";
            public const string AttemptingUpdateProduct = "Controller: Attempting to update product ID: {ProductId}";
            public const string AttemptingPatchProduct = "Controller: Attempting to partially update product ID: {ProductId}";
            public const string AttemptingDeleteProduct = "Controller: Attempting to delete product ID: {ProductId}";
            public const string ProductFoundInCache = "Product ID: {ProductId} found in cache.";
            public const string ProductNotInCacheFetching = "Product ID: {ProductId} not found in cache. Fetching from repository.";
            public const string ProductFetchedAndCached = "Product ID: {ProductId} fetched from repository and cached.";
            public const string CacheInvalidatedForProduct = "Cache invalidated for Product ID: {ProductId} due to {Reason}.";
            public const string ControllerProductNotFoundById = "Controller: Product with ID {ProductId} not found in repository.";
            public const string ControllerProductNotFoundByName = "Controller: Product with name '{ProductName}' not found.";
            public const string ControllerProductCreateFailed = "Controller: Failed to create product {ProductName}. Repository returned null.";
            public const string ControllerProductUpdateNotFound = "Controller: Product with ID {ProductId} not found for update.";
            public const string ControllerProductPatchFailed = "Controller: Failed to update product ID {ProductId} after patch. Repository returned null.";
            public const string ControllerProductDeleteFailed = "Controller: Product ID {ProductId} not found for deletion or delete failed.";
            public const string RepoFetchingProducts = "Repository: Fetching products. Colour: {Colour}";
            public const string RepoFetchedProducts = "Repository: Fetched {ItemCount} products.";
            public const string RepoFetchingProductById = "Repository: Fetching product by ID: {ProductId}.";
            public const string RepoProductNotFoundById = "Repository: Product with ID {ProductId} not found.";
            public const string RepoFetchingProductByName = "Repository: Fetching product by name: {ProductName}.";
            public const string RepoProductNotFoundByName = "Repository: Product with name '{ProductName}' not found.";
            public const string RepoCreatingProduct = "Repository: Creating product: {ProductName}.";
            public const string RepoProductCreated = "Repository: Product created. ID: {ProductId}, Name: {ProductName}.";
            public const string RepoErrorCreatingProduct = "Repository: Error creating product {ProductName}.";
            public const string RepoUpdatingProduct = "Repository: Updating product ID: {ProductId}.";
            public const string RepoProductUpdateNotFound = "Repository: Update failed. Product ID {ProductId} not found.";
            public const string RepoProductUpdated = "Repository: Product updated. ID: {ProductId}, Name: {ProductName}.";
            public const string RepoErrorUpdatingProduct = "Repository: Error updating product ID {ProductId}.";
            public const string RepoDeletingProduct = "Repository: Deleting product ID: {ProductId}.";
            public const string RepoProductDeleteNotFound = "Repository: Delete failed. Product ID {ProductId} not found.";
            public const string RepoProductDeleted = "Repository: Product ID: {ProductId} deleted.";
            public const string RepoProductDeleteZeroChanges = "Repository: Product ID {ProductId} delete operation resulted in 0 changes.";
            public const string RepoErrorDeletingProduct = "Repository: Error deleting product ID {ProductId}.";
            public const string EventPublishing = "Publishing ProductChangedEvent: {EventJson}";
            public const string ApplyMigrations = "Attempting to apply migrations and/or seed data...";
            public const string RelationalDbDetected = "Relational database detected. Checking for pending migrations.";
            public const string ApplyingMigrations = "Applying database migrations...";
            public const string MigrationsApplied = "Database migrations applied successfully.";
            public const string NoPendingMigrations = "No pending database migrations.";
            public const string NonRelationalDbDetected = "Non-relational/InMemory database detected. Ensuring database is created.";
            public const string DbEnsuredCreated = "Database ensured/created.";
            public const string DbSetupError = "An error occurred during database setup.";
            public const string GeneratedTestToken = "Generated test admin token using Issuer: {Issuer}, Audience: {Audience}";
            public const string AppStartingUp = ApplicationName + " starting up..."; // Reference local const
            public const string AppShuttingDown = ApplicationName + " shutting down...";
        }

        // Exception Message Templates specific to ProductAPI (or if shared, move to AppConstants)
        public static class ExceptionMessages
        {
            public const string JwtSettingsInvalidFormat = "JWT ApiSettings configuration is invalid: {0}. Application cannot start.";
            public const string TestTokenJwtSecretInvalid = "Test token generation cannot proceed due to invalid ApiSettings.Secret.";
            public const string CriticalErrorJwtSettingsInvalidConsoleFormat = "CRITICAL ERROR: JWT ApiSettings configuration is invalid: {0}. Check section '{1}'.";
            public const string CriticalErrorJwtSecretForTestTokenInvalidFormat = "CRITICAL: JWT Secret for test token endpoint is invalid or too short. Configured Secret (partial): '{0}'. Check configuration.";
            public const string CriticalConfigValidationFailedFormat = "CRITICAL: Configuration validation failed. Application cannot start. Errors: {0}";
            public const string CriticalHostTerminatedUnexpectedly = "CRITICAL: " + ApplicationName + " host terminated unexpectedly.";
        }

        // ProblemDetails constants specific to ProductAPI
        public static class ProblemDetails
        {
            public static class Titles
            {
                public const string ProductNotFound = "Product not found";
                public const string ValidationError = "One or more validation errors occurred.";
                public const string ProductCreationError = "Error creating product";
                public const string ProductIdMismatch = "Product ID Mismatch";
                public const string PatchDocumentRequired = "A JSON patch document is required.";
                public const string PatchUpdateError = "Error updating product after patch";
                public const string GenericError = "An unexpected error occurred.";
                public const string Unauthorized = "Unauthorized";
            }

            public static class DetailFormats
            {
                public const string ProductWithIdNotFound = "Product with ID {0} could not be found.";
                public const string ProductWithNameNotFound = "Product with name '{0}' could not be found.";
                public const string ProductUpdateNotFound = "Product with ID {0} could not be found for update.";
                public const string ProductPatchNotFound = "Product with ID {0} not found for patching.";
                public const string ProductDeleteNotFound = "Product with ID {0} could not be found or an error occurred during deletion.";
                public const string ProductIdMismatch = "Product ID in the URL must match the Product ID in the request body, or the body ProductID can be 0.";
                public const string GenericCreateError = Titles.GenericError + " while trying to create the product.";
                public const string GenericPatchError = Titles.GenericError + " while trying to save the patched product.";
                public const string ValidationAfterPatch = Titles.ValidationError + " after applying the patch.";
            }
        }
    }
}