using FluentValidation.TestHelper;
using VaccinationControl.Application.Vaccinations.Commands.RegisterVaccination;
using VaccinationControl.Domain.Enums;

namespace VaccinationControl.UnitTests.Vaccinations.Commands.RegisterVaccination
{
    /// <summary>
    /// Cobre RN01 e RN02 — as únicas regras de dose que dependem só do formato da
    /// requisição. As demais exigem o estado gravado e são testadas no handler.
    /// </summary>
    public class RegisterVaccinationCommandValidatorTests
    {
        private readonly RegisterVaccinationCommandValidator _validator = new();

        private static RegisterVaccinationCommand Comando(
            int doseNumber = 1,
            DateOnly? vaccinationDate = null,
            VaccinationTypeEnum vaccinationType = VaccinationTypeEnum.Dose)
        {
            return new RegisterVaccinationCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                vaccinationType,
                doseNumber,
                vaccinationDate ?? DateOnly.FromDateTime(DateTime.UtcNow));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void RN01_deve_recusar_dose_nao_positiva(int doseNumber)
        {
            var result = _validator.TestValidate(Comando(doseNumber: doseNumber));

            result.ShouldHaveValidationErrorFor(command => command.DoseNumber);
        }

        [Fact]
        public void RN02_deve_recusar_data_futura()
        {
            var amanha = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            var result = _validator.TestValidate(Comando(vaccinationDate: amanha));

            result.ShouldHaveValidationErrorFor(command => command.VaccinationDate);
        }

        [Fact]
        public void RN02_deve_aceitar_data_de_hoje()
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            var result = _validator.TestValidate(Comando(vaccinationDate: hoje));

            result.ShouldNotHaveValidationErrorFor(command => command.VaccinationDate);
        }

        [Fact]
        public void Deve_recusar_tipo_fora_do_enum()
        {
            var command = Comando(vaccinationType: (VaccinationTypeEnum)99);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.VaccinationType);
        }

        [Fact]
        public void Deve_recusar_identificadores_vazios()
        {
            var command = new RegisterVaccinationCommand(
                Guid.Empty,
                Guid.Empty,
                VaccinationTypeEnum.Dose,
                1,
                DateOnly.FromDateTime(DateTime.UtcNow));

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.PersonId);
            result.ShouldHaveValidationErrorFor(command => command.VaccineId);
        }

        [Fact]
        public void Deve_aceitar_requisicao_valida()
        {
            var result = _validator.TestValidate(Comando());

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
