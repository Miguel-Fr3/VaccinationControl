using System.Text.Json;
using FluentAssertions;
using VaccinationControl.IntegrationTests.Support;

namespace VaccinationControl.IntegrationTests
{
    /// <summary>
    /// O documento OpenAPI é a única descrição da API que chega a quem a consome pelo Scalar.
    /// Declarar o esquema de segurança em Components apenas o cataloga — sem o requisito por
    /// operação, as rotas protegidas aparecem como públicas, e nada no runtime denuncia isso.
    /// </summary>
    public class OpenApiDocumentTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private const string SchemeName = "Session";

        [Fact]
        public async Task Documento_deve_catalogar_o_esquema_de_sessao_como_cookie()
        {
            using var documento = await ObterDocumentoAsync();

            var esquema = documento.RootElement
                .GetProperty("components")
                .GetProperty("securitySchemes")
                .GetProperty(SchemeName);

            esquema.GetProperty("type").GetString().Should().Be("apiKey");
            esquema.GetProperty("in").GetString().Should().Be("cookie");
            esquema.GetProperty("name").GetString().Should().Be("vaccination-control-auth");
        }

        [Theory]
        [InlineData("/api/vaccines", "get")]
        [InlineData("/api/vaccines", "post")]
        [InlineData("/api/people", "get")]
        [InlineData("/api/auth/me", "get")]
        public async Task Rota_protegida_deve_exigir_a_sessao(string rota, string metodo)
        {
            using var documento = await ObterDocumentoAsync();

            var requisitos = documento.RootElement
                .GetProperty("paths")
                .GetProperty(rota)
                .GetProperty(metodo)
                .GetProperty("security");

            requisitos.EnumerateArray()
                .Should().ContainSingle()
                .Which.TryGetProperty(SchemeName, out _).Should().BeTrue();
        }

        [Theory]
        [InlineData("/api/auth/login")]
        [InlineData("/api/auth/register")]
        [InlineData("/api/auth/logout")]
        public async Task Rota_anonima_nao_deve_exigir_a_sessao(string rota)
        {
            // O 'me' é a contraprova destes três: mesma controller, e só ele exige sessão.
            using var documento = await ObterDocumentoAsync();

            var operacao = documento.RootElement
                .GetProperty("paths")
                .GetProperty(rota)
                .GetProperty("post");

            operacao.TryGetProperty("security", out _).Should().BeFalse();
        }

        private async Task<JsonDocument> ObterDocumentoAsync()
        {
            var client = factory.CreateClient();

            var corpo = await client.GetStringAsync("/openapi/v1.json");

            return JsonDocument.Parse(corpo);
        }
    }
}
