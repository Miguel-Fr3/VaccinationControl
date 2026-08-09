using MediatR;

namespace VaccinationControl.Application.Vaccines.Commands.CreateVaccine
{
    public record CreateVaccineCommand(string Name) : IRequest<VaccineResponse>;
}
