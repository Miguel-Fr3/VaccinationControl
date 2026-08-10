using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Mappers
{
    public class PersonMapper : EntityBaseMapper<Person>
    {
        protected override void MapEntity(EntityTypeBuilder<Person> builder)
        {
            builder.ToTable("People");

            builder.Property(person => person.Name)
                .HasMaxLength(200)
                .IsRequired();

            // O documento tem tamanho fixo; a coluna acompanha a regra dos validators.
            builder.Property(person => person.Document)
                .HasMaxLength(11)
                .IsRequired();

            // O documento é o número de identificação único da pessoa.
            builder.HasIndex(person => person.Document)
                .IsUnique();

            // Remover a pessoa apaga o cartão de vacinação inteiro.
            builder.HasMany(person => person.VaccinationRecords)
                .WithOne(record => record.Person)
                .HasForeignKey(record => record.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
