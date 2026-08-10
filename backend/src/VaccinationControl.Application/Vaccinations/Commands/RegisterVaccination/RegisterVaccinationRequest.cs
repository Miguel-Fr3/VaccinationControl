using VaccinationControl.Domain.Enums;

namespace VaccinationControl.Application.Vaccinations.Commands.RegisterVaccination
{
    /// <summary>
    /// Corpo da requisição de registro. Separado do command porque o identificador da
    /// pessoa vem da rota, não do corpo — assim ele não aparece duplicado no contrato.
    /// </summary>
    public record RegisterVaccinationRequest(
        Guid VaccineId,
        VaccinationTypeEnum VaccinationType,
        int DoseNumber,
        DateOnly VaccinationDate);
}
