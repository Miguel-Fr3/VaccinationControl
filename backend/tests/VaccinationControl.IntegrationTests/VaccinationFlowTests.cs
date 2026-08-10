using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VaccinationControl.IntegrationTests.Support;

namespace VaccinationControl.IntegrationTests
{
    /// <summary>
    /// Percorre o fluxo de ponta a ponta, pela API real: cadastrar vacina e pessoa,
    /// registrar doses, consultar o cartão e remover. O que se verifica aqui são os
    /// comportamentos que nenhum teste unitário alcança — persistência, cascata e status HTTP.
    /// </summary>
    public class VaccinationFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private record IdResponse(Guid Id);

        private record DoseResponse(Guid RecordId, string VaccinationType, int DoseNumber);

        private record VacinaDoCartao(Guid VaccineId, string VaccineName, int TotalDoses, DoseResponse[] Doses);

        private record CartaoResponse(Guid PersonId, string PersonName, string Document, VacinaDoCartao[] Vaccines);

        private static async Task<Guid> CadastrarVacinaAsync(HttpClient client, string? nome = null)
        {
            var resposta = await client.PostAsJsonAsync(
                "/api/vaccines",
                new { name = nome ?? $"Vacina {Guid.NewGuid():N}" });

            resposta.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await resposta.Content.ReadFromJsonAsync<IdResponse>(ApiClient.Json))!.Id;
        }

        private static async Task<Guid> CadastrarPessoaAsync(HttpClient client)
        {
            var documento = Random.Shared.NextInt64(10000000000, 99999999999).ToString();

            var resposta = await client.PostAsJsonAsync(
                "/api/people",
                new { name = "Maria Silva", document = documento });

            resposta.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await resposta.Content.ReadFromJsonAsync<IdResponse>(ApiClient.Json))!.Id;
        }

        private static Task<HttpResponseMessage> RegistrarDoseAsync(
            HttpClient client,
            Guid personId,
            Guid vaccineId,
            int doseNumber,
            string vaccinationType = "Dose",
            string vaccinationDate = "2024-01-10")
        {
            return client.PostAsJsonAsync(
                $"/api/people/{personId}/vaccinations",
                new { vaccineId, vaccinationType, doseNumber, vaccinationDate });
        }

        [Fact]
        public async Task Fluxo_feliz_deve_ir_do_cadastro_ao_cartao()
        {
            var client = await factory.AutenticadoAsync();
            var vaccineId = await CadastrarVacinaAsync(client, "Hepatite B");
            var personId = await CadastrarPessoaAsync(client);

            (await RegistrarDoseAsync(client, personId, vaccineId, 1, vaccinationDate: "2024-01-10"))
                .StatusCode.Should().Be(HttpStatusCode.Created);

            (await RegistrarDoseAsync(client, personId, vaccineId, 2, vaccinationDate: "2024-03-10"))
                .StatusCode.Should().Be(HttpStatusCode.Created);

            var cartao = await client.GetFromJsonAsync<CartaoResponse>(
                $"/api/people/{personId}/vaccination-card",
                ApiClient.Json);

            cartao!.Vaccines.Should().HaveCount(1);
            cartao.Vaccines[0].VaccineName.Should().Be("Hepatite B");
            cartao.Vaccines[0].TotalDoses.Should().Be(2);
        }

        [Fact]
        public async Task Cartao_de_pessoa_sem_doses_deve_vir_vazio()
        {
            var client = await factory.AutenticadoAsync();
            var personId = await CadastrarPessoaAsync(client);

            var cartao = await client.GetFromJsonAsync<CartaoResponse>(
                $"/api/people/{personId}/vaccination-card",
                ApiClient.Json);

            cartao!.Vaccines.Should().BeEmpty();
        }

        [Fact]
        public async Task RN06_dose_2_sem_a_1_deve_responder_409()
        {
            var client = await factory.AutenticadoAsync();
            var vaccineId = await CadastrarVacinaAsync(client);
            var personId = await CadastrarPessoaAsync(client);

            var resposta = await RegistrarDoseAsync(client, personId, vaccineId, 2);

            resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task RN05_dose_duplicada_deve_responder_409()
        {
            var client = await factory.AutenticadoAsync();
            var vaccineId = await CadastrarVacinaAsync(client);
            var personId = await CadastrarPessoaAsync(client);

            await RegistrarDoseAsync(client, personId, vaccineId, 1);
            var repetida = await RegistrarDoseAsync(client, personId, vaccineId, 1);

            repetida.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Numeracao_por_tipo_deve_permitir_dose_1_normal_e_reforco_1()
        {
            var client = await factory.AutenticadoAsync();
            var vaccineId = await CadastrarVacinaAsync(client);
            var personId = await CadastrarPessoaAsync(client);

            await RegistrarDoseAsync(client, personId, vaccineId, 1);

            var reforco = await RegistrarDoseAsync(
                client, personId, vaccineId, 1, "BoosterDose", "2024-06-10");

            // O índice único do banco cobre (pessoa, vacina, tipo, dose): sem o tipo na chave,
            // esta inserção violaria a constraint e viraria 409.
            reforco.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task RN08_reforco_sem_dose_normal_deve_responder_409()
        {
            var client = await factory.AutenticadoAsync();
            var vaccineId = await CadastrarVacinaAsync(client);
            var personId = await CadastrarPessoaAsync(client);

            var resposta = await RegistrarDoseAsync(
                client, personId, vaccineId, 1, "BoosterDose");

            resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task RN03_pessoa_inexistente_deve_responder_404()
        {
            var client = await factory.AutenticadoAsync();
            var vaccineId = await CadastrarVacinaAsync(client);

            var resposta = await RegistrarDoseAsync(client, Guid.NewGuid(), vaccineId, 1);

            resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RN04_vacina_inexistente_deve_responder_404()
        {
            var client = await factory.AutenticadoAsync();
            var personId = await CadastrarPessoaAsync(client);

            var resposta = await RegistrarDoseAsync(client, personId, Guid.NewGuid(), 1);

            resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Documento_duplicado_deve_responder_409()
        {
            var client = await factory.AutenticadoAsync();
            var documento = Random.Shared.NextInt64(10000000000, 99999999999).ToString();

            await client.PostAsJsonAsync("/api/people", new { name = "Maria", document = documento });
            var segunda = await client.PostAsJsonAsync(
                "/api/people",
                new { name = "Outra Pessoa", document = documento });

            segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Remover_pessoa_deve_apagar_o_cartao_em_cascata()
        {
            var client = await factory.AutenticadoAsync();
            var vaccineId = await CadastrarVacinaAsync(client);
            var personId = await CadastrarPessoaAsync(client);

            await RegistrarDoseAsync(client, personId, vaccineId, 1);

            var remocao = await client.DeleteAsync($"/api/people/{personId}");
            remocao.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // O ON DELETE CASCADE da FK depende do PRAGMA foreign_keys estar ativo; este
            // teste é o que prova que ele está.
            var cartao = await client.GetAsync($"/api/people/{personId}/vaccination-card");
            cartao.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // A vacina do catálogo não pode ter sido levada junto.
            var vacina = await client.GetAsync($"/api/vaccines/{vaccineId}");
            vacina.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Registro_removido_deve_sumir_do_cartao()
        {
            var client = await factory.AutenticadoAsync();
            var vaccineId = await CadastrarVacinaAsync(client);
            var personId = await CadastrarPessoaAsync(client);

            await RegistrarDoseAsync(client, personId, vaccineId, 1);

            var cartao = await client.GetFromJsonAsync<CartaoResponse>(
                $"/api/people/{personId}/vaccination-card",
                ApiClient.Json);

            var recordId = cartao!.Vaccines[0].Doses[0].RecordId;

            var remocao = await client.DeleteAsync(
                $"/api/people/{personId}/vaccinations/{recordId}");
            remocao.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var depois = await client.GetFromJsonAsync<CartaoResponse>(
                $"/api/people/{personId}/vaccination-card",
                ApiClient.Json);

            depois!.Vaccines.Should().BeEmpty();
        }

        [Fact]
        public async Task Registro_de_outra_pessoa_deve_responder_404()
        {
            var client = await factory.AutenticadoAsync();
            var vaccineId = await CadastrarVacinaAsync(client);
            var personId = await CadastrarPessoaAsync(client);
            var outraPessoaId = await CadastrarPessoaAsync(client);

            await RegistrarDoseAsync(client, personId, vaccineId, 1);

            var cartao = await client.GetFromJsonAsync<CartaoResponse>(
                $"/api/people/{personId}/vaccination-card",
                ApiClient.Json);

            var recordId = cartao!.Vaccines[0].Doses[0].RecordId;

            var resposta = await client.GetAsync(
                $"/api/people/{outraPessoaId}/vaccinations/{recordId}");

            resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
