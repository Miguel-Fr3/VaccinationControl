using MediatR;

namespace VaccinationControl.Application.People.Commands.CreatePerson
{
    public record CreatePersonCommand(string Name, string Document) : IRequest<PersonResponse>;
}
