using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Infrastructure.Persistence;
using VaccinationControl.Infrastructure.Persistence.Repositories;
using VaccinationControl.Infrastructure.Security;

namespace VaccinationControl.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            string contentRootPath)
        {
            var connectionString = ResolveDataSource(
                configuration.GetConnectionString("DefaultConnection"),
                contentRootPath);

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

            // Mesma instância do DbContext atende o repositório e o unit of work no escopo.
            services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

            services.AddScoped<IVaccineRepository, VaccineRepository>();
            services.AddScoped<IPersonRepository, PersonRepository>();
            services.AddScoped<IVaccinationRecordRepository, VaccinationRecordRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            //Singleton porque não tem estado e é thread-safe; o mesmo para o gerador de token.
            services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

            return services;
        }

        /// <summary>
        /// O SQLite resolve um Data Source relativo contra o diretório de trabalho do processo,
        /// que muda conforme a aplicação seja iniciada por <c>dotnet run</c>, pelo executável em
        /// <c>bin/</c> ou pelas ferramentas do EF. Ancorar no content root garante um único banco.
        /// </summary>
        private static string ResolveDataSource(string? connectionString, string contentRootPath)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "A connection string 'DefaultConnection' não foi configurada.");
            }

            var builder = new SqliteConnectionStringBuilder(connectionString);

            if (!Path.IsPathRooted(builder.DataSource))
            {
                builder.DataSource = Path.Combine(contentRootPath, builder.DataSource);
            }

            return builder.ConnectionString;
        }
    }
}
