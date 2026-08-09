using Microsoft.EntityFrameworkCore;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Persistence.Repositories
{
    public class PersonRepository : IPersonRepository
    {
        private readonly AppDbContext _context;

        public PersonRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsByDocumentAsync(
            string document,
            CancellationToken cancellationToken = default)
        {
            return _context.People.AnyAsync(person => person.Document == document, cancellationToken);
        }

        public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.People
                .AsNoTracking()
                .FirstOrDefaultAsync(person => person.Id == id, cancellationToken);
        }

        public void Add(Person person)
        {
            _context.People.Add(person);
        }

        public void Remove(Person person)
        {
            // A entidade vem sem tracking; Remove a anexa como Deleted e o EF emite um
            // DELETE pela chave. Os registros de vacinação caem pela cascata da FK.
            _context.People.Remove(person);
        }
    }
}
