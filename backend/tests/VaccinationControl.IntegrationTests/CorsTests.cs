using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VaccinationControl.IntegrationTests.Support;

namespace VaccinationControl.IntegrationTests
{
    /// <summary>
    /// A política de CORS é invisível para quem chama a API por HttpClient — só o navegador a
    /// aplica. Estes testes ocupam o lugar dele: sem eles, uma regressão na política só
    /// apareceria como requisição bloqueada no console do frontend.
    /// </summary>
    public class CorsTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private const string OrigemDoFrontend = "http://localhost:5173";

        [Fact]
        public async Task Preflight_da_origem_do_frontend_deve_ser_liberado_sem_token()
        {
            // A verificação prévia chega antes de qualquer credencial: se a fallback policy a
            // alcançasse, o navegador receberia 401 e nem faria a requisição real.
            var client = factory.CreateClient();

            var requisicao = new HttpRequestMessage(HttpMethod.Options, "/api/vaccines");
            requisicao.Headers.Add("Origin", OrigemDoFrontend);
            requisicao.Headers.Add("Access-Control-Request-Method", "POST");
            requisicao.Headers.Add("Access-Control-Request-Headers", "content-type");

            var resposta = await client.SendAsync(requisicao);

            resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
            resposta.Headers.GetValues("Access-Control-Allow-Origin")
                .Should().ContainSingle().Which.Should().Be(OrigemDoFrontend);
            resposta.Headers.GetValues("Access-Control-Allow-Methods")
                .Should().ContainSingle().Which.Should().Contain("POST");
        }

        [Fact]
        public async Task Resposta_a_origem_do_frontend_deve_permitir_credenciais()
        {
            // Sem este cabeçalho o navegador descarta a resposta de toda requisição feita com
            // withCredentials — que serão todas, assim que a sessão for para o cookie.
            var client = factory.CreateClient();

            var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { email = "ausente@exemplo.com", password = "senha12345" })
            };
            requisicao.Headers.Add("Origin", OrigemDoFrontend);

            var resposta = await client.SendAsync(requisicao);

            resposta.Headers.GetValues("Access-Control-Allow-Origin")
                .Should().ContainSingle().Which.Should().Be(OrigemDoFrontend);
            resposta.Headers.GetValues("Access-Control-Allow-Credentials")
                .Should().ContainSingle().Which.Should().Be("true");
        }

        [Fact]
        public async Task Origem_desconhecida_nao_deve_receber_liberacao()
        {
            var client = factory.CreateClient();

            var requisicao = new HttpRequestMessage(HttpMethod.Options, "/api/vaccines");
            requisicao.Headers.Add("Origin", "http://site-malicioso.exemplo");
            requisicao.Headers.Add("Access-Control-Request-Method", "POST");

            var resposta = await client.SendAsync(requisicao);

            resposta.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }
    }
}
