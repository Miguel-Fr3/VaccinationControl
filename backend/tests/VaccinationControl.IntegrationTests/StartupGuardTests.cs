using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace VaccinationControl.IntegrationTests
{
    /// <summary>
    /// Configuração ausente ou inválida derruba o startup, e é aqui que isso fica provado. As
    /// duas falhas cobertas eram silenciosas de formas diferentes: a chave curta só aparecia
    /// como 500 no primeiro login, e a lista de origens vazia, como erro de CORS no console do
    /// navegador — nenhuma das duas dizia que faltava configuração.
    /// </summary>
    public class StartupGuardTests
    {
        private const string ChaveValida = "chave-de-teste-com-no-minimo-32-bytes-para-hmac-sha256";

        [Fact]
        public void Chave_JWT_curta_deve_derrubar_o_startup()
        {
            // Menos de 32 bytes é menos que o digest do HMAC-SHA256, que recusa a chave na hora
            // de assinar — longe daqui, e sem mencionar configuração.
            using var factory = new FactoryConfiguravel(("Jwt:Key", "curta-demais"));

            var acao = () => factory.CreateClient();

            acao.Should().Throw<InvalidOperationException>()
                .WithMessage("*Jwt:Key*32 bytes*");
        }

        [Fact]
        public void Chave_JWT_ausente_deve_derrubar_o_startup()
        {
            using var factory = new FactoryConfiguravel(("Jwt:Key", ""));

            var acao = () => factory.CreateClient();

            acao.Should().Throw<InvalidOperationException>()
                .WithMessage("*Jwt:Key*");
        }

        [Fact]
        public void Lista_de_origens_vazia_deve_derrubar_o_startup()
        {
            using var factory = new FactoryConfiguravel(("Cors:AllowedOrigins:0", ""));

            var acao = () => factory.CreateClient();

            acao.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cors:AllowedOrigins*");
        }

        /// <summary>
        /// Sobe o host real com as configurações trocadas. Não herda do <c>ApiFactory</c> de
        /// propósito: ele existe para dar um host que funciona, e estes testes precisam
        /// justamente do que impede o host de existir.
        /// </summary>
        private sealed class FactoryConfiguravel(params (string Chave, string Valor)[] configuracoes)
            : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("Jwt:Key", ChaveValida);

                // Depois do valor válido acima, para que cada teste sobrescreva só o que quer
                // invalidar e o resto da configuração continue de pé.
                foreach (var (chave, valor) in configuracoes)
                {
                    builder.UseSetting(chave, valor);
                }
            }
        }
    }
}
