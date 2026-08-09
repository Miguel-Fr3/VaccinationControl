using MediatR;

namespace VaccinationControl.Application.Vaccines.Queries.GetVaccineById
{
    public record GetVaccineByIdQuery(Guid Id) : IRequest<VaccineResponse>;
}
