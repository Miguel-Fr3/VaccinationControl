using MediatR;

namespace VaccinationControl.Application.Vaccinations.Queries.GetVaccinationRecordById
{
    public record GetVaccinationRecordByIdQuery(Guid PersonId, Guid RecordId)
        : IRequest<VaccinationRecordResponse>;
}
