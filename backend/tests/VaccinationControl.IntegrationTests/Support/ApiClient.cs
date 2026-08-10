using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VaccinationControl.IntegrationTests.Support
{
    /// <summary>
    /// Encapsula o que todo teste precisa antes de chegar no que quer verificar: obter um
    /// token e mandá-lo em cada requisição. Sem isto, a fallback policy global responde 401
    /// antes de qualquer controller.
    /// </summary>
    public static class ApiClient
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Cadastra um usuário novo e devolve um cliente já autenticado. O e-mail é único por
        /// chamada para que testes na mesma classe não colidam no índice único.
        /// </summary>
        public static async Task<HttpClient> AutenticadoAsync(this ApiFactory factory)
        {
            var client = factory.CreateClient();

            var resposta = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email = $"teste-{Guid.NewGuid():N}@exemplo.com", password = "senha12345" });

            resposta.EnsureSuccessStatusCode();

            var autenticacao = await resposta.Content.ReadFromJsonAsync<AutenticacaoResponse>(Json);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", autenticacao!.Token);

            return client;
        }

        public record AutenticacaoResponse(Guid UserId, string Email, string Token, DateTime ExpiresAtUtc);
    }
}
