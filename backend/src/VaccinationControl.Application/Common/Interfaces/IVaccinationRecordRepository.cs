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

        /// <summary>
        /// Todos os registros da pessoa, com a vacina carregada, na ordem em que compõem o
        /// cartão: por nome da vacina, depois tipo, depois número da dose.
        /// </summary>
        Task<IReadOnlyList<VaccinationRecord>> GetByPersonAsync(
            Guid personId,
            CancellationToken cancellationToken = default);

        void Add(VaccinationRecord vaccinationRecord);
    }
}
