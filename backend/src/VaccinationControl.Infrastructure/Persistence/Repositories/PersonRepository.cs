using Microsoft.EntityFrameworkCore;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Repositories
{
    public class PersonRepository(AppDbContext context) : IPersonRepository
    {
        public Task<bool> ExistsByDocumentAsync(
            string document,
            CancellationToken cancellationToken = default)
        {
            return context.People.AnyAsync(person => person.Document == document, cancellationToken);
        }

        public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return context.People
                .AsNoTracking()
                .FirstOrDefaultAsync(person => person.Id == id, cancellationToken);
        }

        public async Task<(IReadOnlyList<Person> Items, int TotalCount)> SearchAsync(
            string? search,
            int? skip,
            int? take,
            CancellationToken cancellationToken = default)
        {
            var query = context.People.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = LikePattern.Contains(search);

                // Nome ou documento: quem busca uma pessoa costuma ter um dos dois em mãos.
                query = query.Where(person =>
                    EF.Functions.Like(person.Name, pattern, LikePattern.EscapeCharacter)
                    || EF.Functions.Like(person.Document, pattern, LikePattern.EscapeCharacter));
            }

            // Total antes do recorte: é o tamanho do resultado filtrado, não da página.
            var totalCount = await query.CountAsync(cancellationToken);

            query = query.OrderBy(person => person.Name);

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

        public void Add(Person person)
        {
            context.People.Add(person);
        }

        public void Remove(Person person)
        {
            // A entidade vem sem tracking; Remove a anexa como Deleted e o EF emite um
            // DELETE pela chave. Os registros de vacinação caem pela cascata da FK.
            context.People.Remove(person);
        }
    }
}
