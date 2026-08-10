using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VaccinationControl.Infrastructure.Persistence;

namespace VaccinationControl.IntegrationTests.Support
{
    /// <summary>
    /// Sobe a API inteira em memória. Troca só duas coisas do host real: o banco, que passa a
    /// ser um SQLite em memória descartado ao fim do teste, e a chave JWT, que em produção vem
    /// de user-secrets e aqui precisa de um valor determinístico.
    /// </summary>
    public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private const string ChaveJwtDeTeste = "chave-de-teste-com-no-minimo-32-bytes-para-hmac-sha256";

        // A conexão fica aberta durante todo o teste: o SQLite em memória descarta o banco
        // assim que a última conexão fecha.
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.UseSetting("Jwt:Key", ChaveJwtDeTeste);

            builder.ConfigureServices(services =>
            {
                // Remove o DbContext apontado para o arquivo e o registra sobre a conexão
                // em memória, mantendo o resto da composição intacto.
                var registros = services
                    .Where(descriptor =>
                        descriptor.ServiceType == typeof(DbContextOptions<AppDbContext>)
                        || descriptor.ServiceType == typeof(AppDbContext))
                    .ToList();

                foreach (var registro in registros)
                {
                    services.Remove(registro);
                }

                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
            });
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync();
        }

        public new async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}
