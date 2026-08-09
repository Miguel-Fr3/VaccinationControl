using FluentValidation;

namespace VaccinationControl.Application.People.Commands.DeletePerson
{
    public class DeletePersonCommandValidator : AbstractValidator<DeletePersonCommand>
    {
        public DeletePersonCommandValidator()
        {
            // Garante que o Id da pessoa não seja nulo ou vazio
            RuleFor(command => command.Id)
                .NotEmpty();
        }
    }
}
