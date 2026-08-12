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
                .IsRequired()
                // A collation padrão do SQLite é BINARY, e com ela "Gripe" e "gripe" são dois
                // nomes distintos: nem o ExistsByNameAsync do handler nem o índice único abaixo
                // percebiam a repetição. Na coluna, e não na consulta, porque é o único lugar
                // que alcança os dois — o índice herda a collation da coluna que indexa.
                .UseCollation("NOCASE");

            builder.HasIndex(vaccine => vaccine.Name)
                .IsUnique();
        }
    }
}
