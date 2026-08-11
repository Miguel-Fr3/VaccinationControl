using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using VaccinationControl.Api.Security;

namespace VaccinationControl.Api.OpenApi
{
    /// <summary>
    /// Declara no documento OpenAPI qual credencial protege a API. Sem isto todos os endpoints
    /// protegidos apareceriam como se fossem públicos.
    /// </summary>
    public class SessionSecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        private const string SchemeName = "Session";

        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Name = AuthCookie.Name,
                Description =
                    "Cookie HttpOnly gravado por POST /api/auth/login ou /api/auth/register "
                    + "e apagado por POST /api/auth/logout."
            };

            return Task.CompletedTask;
        }
    }
}
