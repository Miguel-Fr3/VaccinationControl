using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Application.Common.Interfaces
{
    public interface IVaccineRepository
    {
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<Vaccine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        void Add(Vaccine vaccine);

        void Remove(Vaccine vaccine);

        /// <summary>
        /// Busca vacinas por trecho do nome e devolve a página pedida junto do total de
        /// registros que atendem ao filtro. <paramref name="search"/>, <paramref name="skip"/>
        /// e <paramref name="take"/> são opcionais: nulos significam sem filtro e sem recorte.
        /// </summary>
        Task<(IReadOnlyList<Vaccine> Items, int TotalCount)> SearchAsync(
            string? search,
            int? skip,
            int? take,
            CancellationToken cancellationToken = default);
    }
}
