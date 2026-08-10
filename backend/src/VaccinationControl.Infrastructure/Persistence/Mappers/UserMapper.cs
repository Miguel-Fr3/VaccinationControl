using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Mappers
{
    public class UserMapper : EntityBaseMapper<User>
    {
        protected override void MapEntity(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.Property(user => user.Email)
                .HasMaxLength(200)
                .IsRequired();

            // O e-mail é a credencial de login e precisa ser único.
            builder.HasIndex(user => user.Email)
                .IsUnique();

            builder.Property(user => user.PasswordHash)
                .IsRequired();
        }
    }
}
