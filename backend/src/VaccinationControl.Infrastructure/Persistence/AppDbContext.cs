using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Infrastructure.Persistence
{
    // SaveChangesAsync herdado de DbContext já satisfaz IUnitOfWork.
    public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
        : DbContext(options), IUnitOfWork
    {
        private const int ConstraintErrorCode = 19;
        private const int UniqueViolationCode = 2067;
        private const int PrimaryKeyViolationCode = 1555;
        private const int ForeignKeyViolationCode = 787;

        public DbSet<Person> People => Set<Person>();
        public DbSet<Vaccine> Vaccines => Set<Vaccine>();
        public DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Os handlers verificam duplicidade e uso antes de gravar, mas entre a verificação e
        /// o commit outra requisição pode inserir o mesmo dado ou passar a usar o registro que
        /// está sendo removido. Sem esta tradução, a constraint do banco derrubaria a
        /// requisição com 500 em vez do 409 correto.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            StampAuditFields();

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
            catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
            {
                throw new ConflictException(
                    "A operação conflita com outro registro que depende deste.",
                    exception);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Preenche a auditoria a partir do token, num lugar só. Deixar isso a cargo de cada
        /// handler significaria esquecer em algum — e a origem do dado é sempre a mesma.
        /// Em requisições anônimas (cadastro do primeiro usuário) o autor fica vazio.
        /// </summary>
        private void StampAuditFields()
        {
            var userId = currentUser.Id ?? Guid.Empty;

            foreach (var entry in ChangeTracker.Entries<EntityBase>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = userId;
                }
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException is SqliteException sqliteException
                && sqliteException.SqliteErrorCode == ConstraintErrorCode
                && (sqliteException.SqliteExtendedErrorCode == UniqueViolationCode
                    || sqliteException.SqliteExtendedErrorCode == PrimaryKeyViolationCode);
        }

        private static bool IsForeignKeyViolation(DbUpdateException exception)
        {
            return exception.InnerException is SqliteException sqliteException
                && sqliteException.SqliteErrorCode == ConstraintErrorCode
                && sqliteException.SqliteExtendedErrorCode == ForeignKeyViolationCode;
        }
    }
}
