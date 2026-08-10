using VaccinationControl.Domain.Enums;

namespace VaccinationControl.Application.Vaccinations.Queries.GetVaccinationCard
{
    /// <summary>
    /// Uma aplicação registrada no cartão. O <c>RecordId</c> é o que o cliente usa para
    /// remover este registro específico.
    /// </summary>
    public record VaccinationCardDoseResponse(
        Guid RecordId,
        VaccinationTypeEnum VaccinationType,
        int DoseNumber,
        DateOnly VaccinationDate);
}
