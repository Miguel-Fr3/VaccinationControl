using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VaccinationControl.IntegrationTests.Support;

namespace VaccinationControl.IntegrationTests
{
    /// <summary>
    /// Busca e paginação passam por SQL de verdade — o LIKE com escape e o Skip/Take só
    /// existem no provider, então nenhum teste unitário os exercita.
    /// </summary>
    public class ListingTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private record PagedResponse<T>(T[] Items, int Page, int PageSize, int TotalCount, int TotalPages);

        private record VacinaResponse(Guid Id, string Name);

        private static async Task CadastrarVacinasAsync(HttpClient client, params string[] nomes)
        {
            foreach (var nome in nomes)
            {
                var resposta = await client.PostAsJsonAsync("/api/vaccines", new { name = nome });

                resposta.StatusCode.Should().Be(HttpStatusCode.Created);
            }
        }

        [Fact]
        public async Task Sem_parametros_deve_devolver_tudo_no_envelope()
        {
            var client = await factory.AutenticadoAsync();
            var prefixo = $"Lista{Guid.NewGuid():N}";

            await CadastrarVacinasAsync(client, $"{prefixo} A", $"{prefixo} B");

            var pagina = await client.GetFromJsonAsync<PagedResponse<VacinaResponse>>(
                $"/api/vaccines?search={prefixo}",
                ApiClient.Json);

            pagina!.TotalCount.Should().Be(2);
            pagina.Items.Should().HaveCount(2);
            // Sem paginação pedida, PageSize reflete o total devolvido.
            pagina.PageSize.Should().Be(2);
            pagina.TotalPages.Should().Be(1);
        }

        [Fact]
        public async Task Deve_paginar_e_calcular_o_total_de_paginas()
        {
            var client = await factory.AutenticadoAsync();
            var prefixo = $"Pag{Guid.NewGuid():N}";

            await CadastrarVacinasAsync(client, $"{prefixo} A", $"{prefixo} B", $"{prefixo} C");

            var pagina = await client.GetFromJsonAsync<PagedResponse<VacinaResponse>>(
                $"/api/vaccines?search={prefixo}&page=2&pageSize=2",
                ApiClient.Json);

            pagina!.TotalCount.Should().Be(3);
            pagina.TotalPages.Should().Be(2);
            pagina.Page.Should().Be(2);
            pagina.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task Busca_nao_deve_diferenciar_maiusculas()
        {
            var client = await factory.AutenticadoAsync();
            var prefixo = $"Caixa{Guid.NewGuid():N}";

            await CadastrarVacinasAsync(client, $"{prefixo} Alta");

            var pagina = await client.GetFromJsonAsync<PagedResponse<VacinaResponse>>(
                $"/api/vaccines?search={prefixo.ToUpperInvariant()}",
                ApiClient.Json);

            pagina!.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task Curinga_no_termo_deve_ser_buscado_literalmente()
        {
            // Sem o escape, '%' viraria "qualquer coisa" e traria o catálogo inteiro.
            var client = await factory.AutenticadoAsync();

            await CadastrarVacinasAsync(client, $"Curinga {Guid.NewGuid():N}");

            var pagina = await client.GetFromJsonAsync<PagedResponse<VacinaResponse>>(
                "/api/vaccines?search=%25",
                ApiClient.Json);

            pagina!.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Pessoas_devem_ser_buscaveis_por_documento()
        {
            var client = await factory.AutenticadoAsync();
            var documento = CpfGenerator.Next();

            await client.PostAsJsonAsync(
                "/api/people",
                new { name = "Buscavel Por Documento", document = documento });

            var pagina = await client.GetFromJsonAsync<JsonElement>(
                $"/api/people?search={documento}",
                ApiClient.Json);

            pagina.GetProperty("totalCount").GetInt32().Should().Be(1);
        }

        [Theory]
        [InlineData("pageSize=0")]
        [InlineData("pageSize=101")]
        [InlineData("page=0")]
        public async Task Deve_recusar_parametros_fora_dos_limites(string querystring)
        {
            var client = await factory.AutenticadoAsync();

            var resposta = await client.GetAsync($"/api/vaccines?{querystring}");

            resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
