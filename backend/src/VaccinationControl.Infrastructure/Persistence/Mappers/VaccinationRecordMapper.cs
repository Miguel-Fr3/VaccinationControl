using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Mappers
{
    public class VaccinationRecordMapper : EntityBaseMapper<VaccinationRecord>
    {
        protected override void MapEntity(EntityTypeBuilder<VaccinationRecord> builder)
        {
            builder.ToTable("VaccinationRecords");

            builder.Property(record => record.VaccinationType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(record => record.DoseNumber)
                .IsRequired();

            builder.Property(record => record.VaccinationDate)
                .IsRequired();

            // Defesa em profundidade para a regra de dose duplicada. O tipo entra na chave
            // porque doses normais e reforços têm numeração independente: a dose 1 normal e
            // a dose 1 de reforço são registros distintos e legítimos.
            builder.HasIndex(record => new
            {
                record.PersonId,
                record.VaccineId,
                record.VaccinationType,
                record.DoseNumber
            }).IsUnique();

            // Uma vacina em uso não pode ser removida junto com o registro.
            builder.HasOne(record => record.Vaccine)
                .WithMany()
                .HasForeignKey(record => record.VaccineId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
