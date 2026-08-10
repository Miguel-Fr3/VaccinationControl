using VaccinationControl.Domain.Enums;

namespace VaccinationControl.Application.Vaccinations
{
    /// <summary>
    /// Um registro de vacinação no contrato da API. Traz o nome da vacina para que o
    /// cliente não precise de uma segunda chamada só para exibi-lo.
    /// </summary>
    public record VaccinationRecordResponse(
        Guid Id,
        Guid PersonId,
        Guid VaccineId,
        string VaccineName,
        VaccinationTypeEnum VaccinationType,
        int DoseNumber,
        DateOnly VaccinationDate);
}
