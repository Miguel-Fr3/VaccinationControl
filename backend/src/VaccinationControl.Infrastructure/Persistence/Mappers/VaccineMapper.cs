using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Mappers
{
    public class VaccineMapper : EntityBaseMapper<Vaccine>
    {
        protected override void MapEntity(EntityTypeBuilder<Vaccine> builder)
        {
            builder.ToTable("Vaccines");

            builder.Property(vaccine => vaccine.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.HasIndex(vaccine => vaccine.Name)
                .IsUnique();
        }
    }
}
