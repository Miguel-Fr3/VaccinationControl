using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VaccinationControl.IntegrationTests.Support
{
    /// <summary>
    /// Encapsula o que todo teste precisa antes de chegar no que quer verificar: abrir uma
    /// sessão. Sem isto, a fallback policy global responde 401 antes de qualquer controller.
    /// </summary>
    public static class ApiClient
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Cadastra um usuário novo e devolve um cliente com a sessão aberta. Não há token a
        /// manipular: o HttpClient do WebApplicationFactory guarda o cookie da resposta e o
        /// reenvia sozinho, do mesmo jeito que o navegador faria. O e-mail é único por chamada
        /// para que testes na mesma classe não colidam no índice único.
        /// </summary>
        public static async Task<HttpClient> AutenticadoAsync(this ApiFactory factory)
        {
            var client = factory.CreateClient();

            var resposta = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email = $"teste-{Guid.NewGuid():N}@exemplo.com", password = "senha12345" });

            resposta.EnsureSuccessStatusCode();

            return client;
        }

        public record SessaoResponse(Guid UserId, string Email);
    }
}
