using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Application.Common.Interfaces
{
    public interface IPersonRepository
    {
        Task<bool> ExistsByDocumentAsync(string document, CancellationToken cancellationToken = default);

        Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        void Add(Person person);

        void Remove(Person person);
    }
}
