using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace VaccinationControl.Api.OpenApi
{
    /// <summary>
    /// Exige o esquema de sessão em cada operação protegida. Declarar o esquema em
    /// <c>Components</c> apenas o cataloga: sem o requisito na operação, a documentação
    /// mostra as quinze rotas como se nenhuma delas precisasse de sessão.
    /// </summary>
    public class SessionSecurityRequirementTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            // A autorização vem da fallback policy global, e não de um [Authorize] por action:
            // aqui a regra é a mesma do runtime — protegido, a menos que se declare anônimo.
            var isAnonymous = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<IAllowAnonymous>()
                .Any();

            if (isAnonymous)
            {
                return Task.CompletedTask;
            }

            operation.Security ??= [];

            // A referência precisa do documento hospedeiro: sem ele o nome do esquema não
            // resolve e o requisito é serializado como um objeto vazio.
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    SessionSecuritySchemeTransformer.SchemeName,
                    context.Document)] = []
            });

            return Task.CompletedTask;
        }
    }
}
