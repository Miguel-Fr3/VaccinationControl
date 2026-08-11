using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VaccinationControl.IntegrationTests.Support;

namespace VaccinationControl.IntegrationTests
{
    /// <summary>
    /// A sessão vive num cookie HttpOnly, e as marcas dele são o que separa a escolha do seu
    /// ganho: sem <c>HttpOnly</c> o token volta a ser legível por script, e sem
    /// <c>SameSite=Lax</c> o navegador o anexa em requisição partida de outro site.
    /// </summary>
    public class SessionCookieTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Login_deve_gravar_o_token_em_cookie_protegido()
        {
            var client = factory.CreateClient();
            var email = $"cookie-{Guid.NewGuid():N}@exemplo.com";

            await client.PostAsJsonAsync("/api/auth/register", new { email, password = "senha12345" });

            var resposta = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { email, password = "senha12345" });

            var cookie = resposta.Headers.GetValues("Set-Cookie").Single();

            cookie.Should().StartWith("vaccination-control-auth=");
            cookie.Should().ContainEquivalentOf("httponly");
            cookie.Should().ContainEquivalentOf("secure");
            cookie.Should().ContainEquivalentOf("samesite=lax");
            cookie.Should().ContainEquivalentOf("path=/");
            // A validade acompanha a do token: sem expires, o cookie morreria ao fechar a aba.
            cookie.Should().ContainEquivalentOf("expires=");
        }

        [Fact]
        public async Task Nenhuma_resposta_de_autenticacao_deve_conter_o_token()
        {
            // Devolver o token no corpo devolveria ao JavaScript exatamente o que o cookie
            // HttpOnly tirou dele.
            var client = factory.CreateClient();
            var email = $"sem-token-{Guid.NewGuid():N}@exemplo.com";

            var cadastro = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email, password = "senha12345" });

            var login = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { email, password = "senha12345" });

            foreach (var resposta in new[] { cadastro, login })
            {
                var corpo = await resposta.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(corpo);

                json.RootElement.TryGetProperty("token", out _).Should().BeFalse();
                json.RootElement.EnumerateObject()
                    .Select(propriedade => propriedade.Name)
                    .Should().BeEquivalentTo("userId", "email");
            }
        }

        [Fact]
        public async Task Cookie_da_sessao_deve_autenticar_sem_cabecalho_Authorization()
        {
            var client = await factory.AutenticadoAsync();

            var resposta = await client.GetAsync("/api/vaccines");

            client.DefaultRequestHeaders.Authorization.Should().BeNull();
            resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Me_deve_devolver_quem_esta_autenticado()
        {
            var client = factory.CreateClient();
            var email = $"quem-sou-{Guid.NewGuid():N}@exemplo.com";

            var cadastro = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email, password = "senha12345" });

            var cadastrado = await cadastro.Content
                .ReadFromJsonAsync<ApiClient.SessaoResponse>(ApiClient.Json);

            var resposta = await client.GetAsync("/api/auth/me");

            resposta.StatusCode.Should().Be(HttpStatusCode.OK);

            var sessao = await resposta.Content
                .ReadFromJsonAsync<ApiClient.SessaoResponse>(ApiClient.Json);

            sessao!.UserId.Should().Be(cadastrado!.UserId);
            sessao.Email.Should().Be(email);
        }

        [Fact]
        public async Task Me_sem_sessao_deve_responder_401()
        {
            // É por este 401 que a interface descobre que precisa mandar ao login.
            var client = factory.CreateClient();

            var resposta = await client.GetAsync("/api/auth/me");

            resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_deve_apagar_o_cookie_e_encerrar_o_acesso()
        {
            var client = await factory.AutenticadoAsync();

            var logout = await client.PostAsync("/api/auth/logout", content: null);

            logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var depois = await client.GetAsync("/api/vaccines");

            depois.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_sem_sessao_deve_funcionar()
        {
            // Quem chega com o cookie já vencido continua precisando que ele seja apagado.
            var client = factory.CreateClient();

            var resposta = await client.PostAsync("/api/auth/logout", content: null);

            resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}
