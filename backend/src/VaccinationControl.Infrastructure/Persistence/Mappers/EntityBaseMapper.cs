using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Mappers
{
    /// <summary>
    /// Concentra o mapeamento dos campos herdados de <see cref="EntityBase"/> para que
    /// cada mapper concreto trate apenas do que é próprio da sua entidade.
    /// </summary>
    public abstract class EntityBaseMapper<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : EntityBase
    {
        public void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(entity => entity.Id);

            // O Id é gerado no construtor de EntityBase, não pelo banco.
            builder.Property(entity => entity.Id)
                .ValueGeneratedNever();

            builder.Property(entity => entity.CreatedAt)
                .IsRequired();

            builder.Property(entity => entity.IsActive)
                .IsRequired();

            MapEntity(builder);
        }

        protected abstract void MapEntity(EntityTypeBuilder<TEntity> builder);
    }
}
