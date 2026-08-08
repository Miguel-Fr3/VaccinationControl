using Microsoft.EntityFrameworkCore;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Person> People => Set<Person>();
        public DbSet<Vaccine> Vaccines => Set<Vaccine>();
        public DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
