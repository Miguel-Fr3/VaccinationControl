using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VaccinationControl.IntegrationTests.Support;

namespace VaccinationControl.IntegrationTests
{
    /// <summary>
    /// Regras de cadastro que só existem inteiras quando a requisição atravessa o banco: o que
    /// entra pela API é o que fica gravado, e a unicidade do nome da vacina depende da collation
    /// da coluna — nenhum teste unitário alcança isso, porque lá o repositório é um dublê que
    /// compara em memória.
    /// </summary>
    public class RegistrationRulesTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Nome_de_vacina_repetido_em_outra_caixa_deve_responder_409()
        {
            // Com a collation BINARY do SQLite, "Tetano" e "tetano" eram dois registros
            // legítimos: nem a checagem do handler nem o índice único enxergavam a repetição.
            var client = await factory.AutenticadoAsync();

            var primeira = await client.PostAsJsonAsync("/api/vaccines", new { name = "Tetano" });
            primeira.StatusCode.Should().Be(HttpStatusCode.Created);

            var repetida = await client.PostAsJsonAsync("/api/vaccines", new { name = "tetano" });

            repetida.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Theory]
        [InlineData("abcdefghijk")]
        [InlineData("123.456.789")]
        [InlineData("1234567890 ")]
        public async Task CPF_que_nao_seja_onze_digitos_deve_responder_400(string documento)
        {
            var client = await factory.AutenticadoAsync();

            var resposta = await client.PostAsJsonAsync(
                "/api/people",
                new { name = "Maria Silva", document = documento });

            resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CPF_com_espaco_nao_deve_ser_gravado_com_dez_digitos()
        {
            // O caso que o tamanho sozinho não pegava: onze caracteres na entrada, dez dígitos
            // no banco. Se o cadastro passar, a busca pelo CPF digitado não acha a pessoa.
            var client = await factory.AutenticadoAsync();

            await client.PostAsJsonAsync(
                "/api/people",
                new { name = "Maria Silva", document = "1234567890 " });

            var busca = await client.GetAsync("/api/people?search=1234567890");
            var pagina = await busca.Content.ReadFromJsonAsync<PaginaDePessoas>(ApiClient.Json);

            pagina!.TotalCount.Should().Be(0);
        }

        private record PaginaDePessoas(int TotalCount);
    }
}
