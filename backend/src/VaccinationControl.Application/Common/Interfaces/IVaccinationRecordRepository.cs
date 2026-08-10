using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Application.Common.Interfaces
{
    public interface IVaccinationRecordRepository
    {
        /// <summary>
        /// Devolve as doses já registradas de uma vacina para uma pessoa.
        /// </summary>
        Task<IReadOnlyList<VaccinationRecord>> GetDosesAsync(
            Guid personId,
            Guid vaccineId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca um registro garantindo que ele pertence à pessoa informada na rota.
        /// </summary>
        Task<VaccinationRecord?> GetByIdAsync(
            Guid personId,
            Guid recordId,
            CancellationToken cancellationToken = default);

        void Add(VaccinationRecord vaccinationRecord);
    }
}
