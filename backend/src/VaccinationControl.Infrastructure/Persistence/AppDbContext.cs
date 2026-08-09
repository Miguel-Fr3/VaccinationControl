using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Infrastructure.Persistence
{
    // SaveChangesAsync herdado de DbContext já satisfaz IUnitOfWork.
    public class AppDbContext : DbContext, IUnitOfWork
    {
        private const int ConstraintErrorCode = 19;
        private const int UniqueViolationCode = 2067;
        private const int PrimaryKeyViolationCode = 1555;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Person> People => Set<Person>();
        public DbSet<Vaccine> Vaccines => Set<Vaccine>();
        public DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();

        /// <summary>
        /// Os handlers verificam duplicidade antes de gravar, mas entre a verificação e o
        /// commit outra requisição pode inserir o mesmo dado. Sem esta tradução, o índice
        /// único do banco derrubaria a requisição com 500 em vez do 409 correto.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                throw new ConflictException(
                    "A operação conflita com um registro já existente.",
                    exception);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException is SqliteException sqliteException
                && sqliteException.SqliteErrorCode == ConstraintErrorCode
                && (sqliteException.SqliteExtendedErrorCode == UniqueViolationCode
                    || sqliteException.SqliteExtendedErrorCode == PrimaryKeyViolationCode);
        }
    }
}
