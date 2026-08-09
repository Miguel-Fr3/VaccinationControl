using MediatR;

namespace VaccinationControl.Application.People.Commands.DeletePerson
{
    /// <summary>
    /// Remove a pessoa e, por cascata no banco, todo o seu cartão de vacinação.
    /// </summary>
    public record DeletePersonCommand(Guid Id) : IRequest;
}
