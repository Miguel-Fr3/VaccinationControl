using MediatR;

namespace VaccinationControl.Application.Vaccinations.Queries.GetVaccinationCard
{
    public record GetVaccinationCardQuery(Guid PersonId) : IRequest<VaccinationCardResponse>;
}
