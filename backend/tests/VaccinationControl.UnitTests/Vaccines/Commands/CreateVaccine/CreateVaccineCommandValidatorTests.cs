using FluentValidation.TestHelper;
using VaccinationControl.Application.Vaccines.Commands.CreateVaccine;

namespace VaccinationControl.UnitTests.Vaccines.Commands.CreateVaccine
{
    public class CreateVaccineCommandValidatorTests
    {
        private readonly CreateVaccineCommandValidator _validator = new();

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Deve_recusar_nome_vazio(string name)
        {
            var result = _validator.TestValidate(new CreateVaccineCommand(name));

            result.ShouldHaveValidationErrorFor(command => command.Name);
        }

        [Fact]
        public void Deve_recusar_nome_acima_de_200_caracteres()
        {
            var command = new CreateVaccineCommand(new string('a', 201));

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.Name);
        }

        [Fact]
        public void Deve_aceitar_nome_no_limite_de_200_caracteres()
        {
            var command = new CreateVaccineCommand(new string('a', 200));

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Deve_aceitar_nome_valido()
        {
            var result = _validator.TestValidate(new CreateVaccineCommand("Hepatite B"));

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
