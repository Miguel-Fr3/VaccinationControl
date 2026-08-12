using Microsoft.EntityFrameworkCore;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Repositories
{
    public class VaccineRepository(AppDbContext context) : IVaccineRepository
    {
        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return context.Vaccines.AnyAsync(vaccine => vaccine.Name == name, cancellationToken);
        }

        public Task<Vaccine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return context.Vaccines
                .AsNoTracking()
                .FirstOrDefaultAsync(vaccine => vaccine.Id == id, cancellationToken);
        }

        public void Add(Vaccine vaccine)
        {
            context.Vaccines.Add(vaccine);
        }

        public void Remove(Vaccine vaccine)
        {
            // A entidade vem sem tracking; Remove a anexa como Deleted e o EF emite um
            // DELETE pela chave.
            context.Vaccines.Remove(vaccine);
        }

        public async Task<(IReadOnlyList<Vaccine> Items, int TotalCount)> SearchAsync(
            string? search,
            int? skip,
            int? take,
            CancellationToken cancellationToken = default)
        {
            // Consulta somente leitura: sem tracking o EF não monta o change tracker.
            var query = context.Vaccines.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = LikePattern.Contains(search);

                query = query.Where(vaccine =>
                    EF.Functions.Like(vaccine.Name, pattern, LikePattern.EscapeCharacter));
            }

            // Total antes do recorte: é o tamanho do resultado filtrado, não da página.
            var totalCount = await query.CountAsync(cancellationToken);

            query = query.OrderBy(vaccine => vaccine.Name);

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            var items = await query.ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
