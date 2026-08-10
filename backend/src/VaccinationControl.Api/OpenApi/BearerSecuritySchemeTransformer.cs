using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace VaccinationControl.Api.OpenApi
{
    /// <summary>
    /// Declara o esquema Bearer no documento OpenAPI. Sem isto o Scalar não oferece onde
    /// colar o token, e todos os endpoints protegidos aparecem como se fossem públicos.
    /// </summary>
    public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        private const string SchemeName = "Bearer";

        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Token obtido em POST /api/auth/login."
            };

            return Task.CompletedTask;
        }
    }
}
