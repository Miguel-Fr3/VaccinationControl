using FluentValidation;

namespace VaccinationControl.Application.Vaccinations.Commands.RegisterVaccination
{
    /// <summary>
    /// Cobre as regras que dependem apenas do formato da requisição. As que exigem
    /// consultar o estado já gravado (existência, duplicidade, sequência) ficam no handler,
    /// porque resultam em 404 e 409 — e não no 400 que este validator produz.
    /// </summary>
    public class RegisterVaccinationCommandValidator : AbstractValidator<RegisterVaccinationCommand>
    {
        public RegisterVaccinationCommandValidator()
        {
            RuleFor(command => command.PersonId)
                .NotEmpty();

            RuleFor(command => command.VaccineId)
                .NotEmpty();

            RuleFor(command => command.VaccinationType)
                .IsInEnum();

            // A dose precisa ser positiva.
            RuleFor(command => command.DoseNumber)
                .GreaterThan(0);

            // Não é possível registrar uma aplicação futura.
            RuleFor(command => command.VaccinationDate)
                .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow));
        }
    }
}
