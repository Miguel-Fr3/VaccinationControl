using FluentValidation;
using VaccinationControl.Application.Common.Validation;

namespace VaccinationControl.Application.People.Commands.CreatePerson
{
    public class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
    {
        public CreatePersonCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(200)
                .WithName("Nome");

            // A unicidade do documento depende do banco e é verificada no handler.
            // O rótulo é o que o usuário lê: a propriedade continua Document, mas os 11
            // caracteres são um CPF, e é assim que a interface chama o campo.
            RuleFor(command => command.Document)
                // Sem parar na primeira falha, um campo vazio acusaria as três regras de uma
                // vez, e o formulário mostraria "informe o CPF" junto de "CPF não é válido".
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Matches("^[0-9]{11}$")
                .Must(Cpf.IsValid)
                .WithMessage("'{PropertyName}' informado não é válido.")
                .WithName("CPF");
        }
    }
}
