using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaccinationControl.Infrastructure.Persistence;
using VaccinationControl.IntegrationTests.Support;

namespace VaccinationControl.IntegrationTests
{
    /// <summary>
    /// A auditoria é preenchida no SaveChangesAsync a partir do token. Depende de HttpContext
    /// e EF Core ao mesmo tempo, então só um teste de integração a alcança — e foi justamente
    /// aqui que passou despercebido, na primeira versão, que o handler do JWT renomeia a claim
    /// 'sub' e deixava o CreatedBy zerado.
    /// </summary>
    public class AuditTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Deve_gravar_o_usuario_autenticado_em_CreatedBy()
        {
            var client = factory.CreateClient();

            var registro = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email = $"auditoria-{Guid.NewGuid():N}@exemplo.com", password = "senha12345" });

            var autenticacao = await registro.Content
                .ReadFromJsonAsync<ApiClient.AutenticacaoResponse>(ApiClient.Json);

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", autenticacao!.Token);

            var criacao = await client.PostAsJsonAsync(
                "/api/vaccines",
                new { name = $"Auditada {Guid.NewGuid():N}" });

            var vacina = await criacao.Content.ReadFromJsonAsync<JsonElement>(ApiClient.Json);
            var vaccineId = vacina.GetProperty("id").GetGuid();

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var gravada = await context.Vaccines
                .AsNoTracking()
                .SingleAsync(entidade => entidade.Id == vaccineId);

            gravada.CreatedBy.Should().Be(autenticacao.UserId);
            gravada.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task Cadastro_anonimo_de_usuario_deve_ficar_sem_autor()
        {
            // Ninguém está autenticado quando o primeiro usuário se cadastra.
            var client = factory.CreateClient();
            var email = $"anonimo-{Guid.NewGuid():N}@exemplo.com";

            var registro = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email, password = "senha12345" });

            var autenticacao = await registro.Content
                .ReadFromJsonAsync<ApiClient.AutenticacaoResponse>(ApiClient.Json);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var usuario = await context.Users
                .AsNoTracking()
                .SingleAsync(entidade => entidade.Id == autenticacao!.UserId);

            usuario.CreatedBy.Should().Be(Guid.Empty);
        }

        [Fact]
        public async Task Senha_nunca_deve_ser_gravada_em_claro()
        {
            var client = factory.CreateClient();
            var email = $"hash-{Guid.NewGuid():N}@exemplo.com";
            const string senha = "senha12345";

            var registro = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email, password = senha });

            var autenticacao = await registro.Content
                .ReadFromJsonAsync<ApiClient.AutenticacaoResponse>(ApiClient.Json);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var usuario = await context.Users
                .AsNoTracking()
                .SingleAsync(entidade => entidade.Id == autenticacao!.UserId);

            usuario.PasswordHash.Should().NotBe(senha);
            usuario.PasswordHash.Should().NotContain(senha);
        }
    }
}
