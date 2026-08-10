using FluentValidation.TestHelper;
using VaccinationControl.Application.Auth.Commands.RegisterUser;

namespace VaccinationControl.UnitTests.Auth
{
    public class RegisterUserCommandValidatorTests
    {
        private const string SenhaValida = "senha12345";

        private readonly RegisterUserCommandValidator _validator = new();

        [Theory]
        [InlineData("")]
        [InlineData("sem-arroba")]
        [InlineData("@dominio.com")]
        public void Deve_recusar_email_invalido(string email)
        {
            var result = _validator.TestValidate(new RegisterUserCommand(email, SenhaValida));

            result.ShouldHaveValidationErrorFor(command => command.Email);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1234567")] // 7 caracteres
        public void Deve_recusar_senha_com_menos_de_8_caracteres(string password)
        {
            var command = new RegisterUserCommand("admin@exemplo.com", password);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.Password);
        }

        [Fact]
        public void Deve_aceitar_senha_no_limite_de_8_caracteres()
        {
            var command = new RegisterUserCommand("admin@exemplo.com", "12345678");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
