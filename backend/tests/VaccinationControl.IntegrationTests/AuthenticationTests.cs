using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VaccinationControl.IntegrationTests.Support;

namespace VaccinationControl.IntegrationTests
{
    public class AuthenticationTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Theory]
        [InlineData("/api/vaccines")]
        [InlineData("/api/people")]
        public async Task Endpoint_protegido_sem_token_deve_responder_401(string rota)
        {
            var client = factory.CreateClient();

            var resposta = await client.GetAsync(rota);

            resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("/scalar/v1")]
        [InlineData("/openapi/v1.json")]
        public async Task Documentacao_deve_ser_anonima(string rota)
        {
            // Se a fallback policy alcançasse a documentação, não haveria onde obter o token.
            var client = factory.CreateClient();

            var resposta = await client.GetAsync(rota);

            resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Deve_cadastrar_usuario_e_abrir_a_sessao()
        {
            var client = factory.CreateClient();
            var email = $"novo-{Guid.NewGuid():N}@exemplo.com";

            var resposta = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email, password = "senha12345" });

            resposta.StatusCode.Should().Be(HttpStatusCode.Created);

            var sessao = await resposta.Content
                .ReadFromJsonAsync<ApiClient.SessaoResponse>(ApiClient.Json);

            sessao!.UserId.Should().NotBeEmpty();
            sessao.Email.Should().Be(email);
        }

        [Fact]
        public async Task Deve_recusar_email_ja_cadastrado()
        {
            var client = factory.CreateClient();
            var email = $"duplicado-{Guid.NewGuid():N}@exemplo.com";

            await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha12345" });
            var segunda = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email, password = "outrasenha" });

            segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Deve_recusar_senha_curta_no_cadastro()
        {
            var client = factory.CreateClient();

            var resposta = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email = $"curta-{Guid.NewGuid():N}@exemplo.com", password = "123" });

            resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Deve_recusar_login_com_credencial_invalida()
        {
            var client = factory.CreateClient();
            var email = $"login-{Guid.NewGuid():N}@exemplo.com";

            await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha12345" });

            var resposta = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { email, password = "senha-errada" });

            resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Deve_autenticar_com_a_credencial_cadastrada()
        {
            var client = factory.CreateClient();
            var email = $"ok-{Guid.NewGuid():N}@exemplo.com";

            await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha12345" });

            var resposta = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { email, password = "senha12345" });

            resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Sessao_aberta_deve_liberar_endpoint_protegido()
        {
            var client = await factory.AutenticadoAsync();

            var resposta = await client.GetAsync("/api/vaccines");

            resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
