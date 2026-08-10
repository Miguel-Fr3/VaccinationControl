using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Application.Common.Interfaces
{
    public interface IPersonRepository
    {
        Task<bool> ExistsByDocumentAsync(string document, CancellationToken cancellationToken = default);

        Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca pessoas por trecho do nome ou do documento e devolve a página pedida junto
        /// do total que atende ao filtro. Parâmetros nulos significam sem filtro e sem recorte.
        /// </summary>
        Task<(IReadOnlyList<Person> Items, int TotalCount)> SearchAsync(
            string? search,
            int? skip,
            int? take,
            CancellationToken cancellationToken = default);

        void Add(Person person);

        void Remove(Person person);
    }
}
