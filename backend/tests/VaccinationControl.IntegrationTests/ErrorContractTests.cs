using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VaccinationControl.IntegrationTests.Support;

namespace VaccinationControl.IntegrationTests
{
    /// <summary>
    /// O GlobalExceptionHandler só existe na Api e traduz exceção em status HTTP. É o tipo de
    /// comportamento que nenhum teste unitário da Application alcança.
    /// </summary>
    public class ErrorContractTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Erro_deve_usar_o_content_type_da_RFC_9457()
        {
            var client = await factory.AutenticadoAsync();

            var resposta = await client.PostAsJsonAsync("/api/vaccines", new { name = "" });

            resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            resposta.Content.Headers.ContentType!.MediaType
                .Should().Be("application/problem+json");
        }

        [Fact]
        public async Task Erro_de_validacao_deve_trazer_o_dicionario_de_campos()
        {
            // O dicionário de campos é o que permite ao cliente exibir a mensagem de erro
            // no campo correto do formulário. Sem ele, o cliente só poderia exibir a mensagem
            // em um lugar genérico, sem associá-la a um campo específico.
            var client = await factory.AutenticadoAsync();

            var resposta = await client.PostAsJsonAsync("/api/vaccines", new { name = "" });

            var corpo = await resposta.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(corpo);

            json.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
            errors.TryGetProperty("Name", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Mensagem_de_validacao_deve_usar_o_rotulo_em_portugues()
        {
            var client = await factory.AutenticadoAsync();

            var resposta = await client.PostAsJsonAsync("/api/vaccines", new { name = "" });

            var corpo = await resposta.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(corpo);

            // O FluentValidation permite customizar o rótulo do campo, mas não permite customizar a mensagem de erro.
            var mensagem = json.RootElement
                .GetProperty("errors")
                .GetProperty("Name")[0]
                .GetString();

            mensagem.Should().Contain("Nome").And.NotContain("'Name'");
        }

        [Fact]
        public async Task Mensagem_de_validacao_nao_deve_depender_da_cultura_do_processo()
        {
            var client = await factory.AutenticadoAsync();

            var resposta = await client.PostAsJsonAsync("/api/vaccines", new { name = "" });

            var corpo = await resposta.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(corpo);

            var mensagem = json.RootElement
                .GetProperty("errors")
                .GetProperty("Name")[0]
                .GetString();

            mensagem.Should().NotContainEquivalentOf("must");
        }

        [Fact]
        public async Task Deve_agregar_erros_de_campos_diferentes_na_mesma_resposta()
        {
            var client = await factory.AutenticadoAsync();
            var personId = await CadastrarPessoaAsync(client);

            var resposta = await client.PostAsJsonAsync(
                $"/api/people/{personId}/vaccinations",
                new
                {
                    vaccineId = Guid.NewGuid(),
                    vaccinationType = "Dose",
                    doseNumber = 0,
                    vaccinationDate = "2099-01-01"
                });

            var corpo = await resposta.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(corpo);

            var errors = json.RootElement.GetProperty("errors");

            errors.TryGetProperty("DoseNumber", out _).Should().BeTrue();
            errors.TryGetProperty("VaccinationDate", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Conflito_deve_trazer_mensagem_de_negocio_no_detail()
        {
            var client = await factory.AutenticadoAsync();

            await client.PostAsJsonAsync("/api/vaccines", new { name = "Conflitante" });
            var repetida = await client.PostAsJsonAsync("/api/vaccines", new { name = "Conflitante" });

            repetida.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var corpo = await repetida.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(corpo);

            json.RootElement.GetProperty("detail").GetString()
                .Should().Contain("Conflitante");
        }

        [Fact]
        public async Task Identificador_malformado_na_rota_deve_responder_404()
        {
            // A constraint {id:guid} recusa antes de chegar ao controller.
            var client = await factory.AutenticadoAsync();

            var resposta = await client.GetAsync("/api/vaccines/nao-e-um-guid");

            resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Identificador_vazio_deve_responder_400()
        {
            var client = await factory.AutenticadoAsync();

            var resposta = await client.GetAsync($"/api/vaccines/{Guid.Empty}");

            resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private static async Task<Guid> CadastrarPessoaAsync(HttpClient client)
        {
            var documento = CpfGenerator.Next();

            var resposta = await client.PostAsJsonAsync(
                "/api/people",
                new { name = "Maria Silva", document = documento });

            var criada = await resposta.Content.ReadFromJsonAsync<JsonElement>(ApiClient.Json);

            return criada.GetProperty("id").GetGuid();
        }
    }
}
