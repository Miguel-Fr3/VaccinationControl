using MediatR;
using VaccinationControl.Domain.Enums;

namespace VaccinationControl.Application.Vaccinations.Commands.RegisterVaccination
{
    public record RegisterVaccinationCommand(
        Guid PersonId,
        Guid VaccineId,
        VaccinationTypeEnum VaccinationType,
        int DoseNumber,
        DateOnly VaccinationDate) : IRequest<VaccinationRecordResponse>;
}
