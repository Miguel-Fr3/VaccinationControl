using FluentAssertions;
using FluentValidation.TestHelper;
using VaccinationControl.Application.People.Commands.CreatePerson;

namespace VaccinationControl.UnitTests.People.Commands.CreatePerson
{
    public class CreatePersonCommandValidatorTests
    {
        private const string DocumentoValido = "12345678901";

        private readonly CreatePersonCommandValidator _validator = new();

        [Fact]
        public void Deve_recusar_nome_vazio()
        {
            var result = _validator.TestValidate(new CreatePersonCommand("", DocumentoValido));

            result.ShouldHaveValidationErrorFor(command => command.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("123")]            // curto demais
        [InlineData("123456789012")]   // longo demais
        public void Deve_recusar_documento_fora_de_11_caracteres(string document)
        {
            var result = _validator.TestValidate(new CreatePersonCommand("Maria Silva", document));

            result.ShouldHaveValidationErrorFor(command => command.Document);
        }

        [Fact]
        public void Mensagem_do_documento_deve_chamar_o_campo_de_CPF()
        {
            // O rótulo é o que o usuário lê, e a interface chama o campo de CPF. Divergir
            // faria a mesma coisa aparecer com dois nomes na mesma tela.
            var result = _validator.TestValidate(new CreatePersonCommand("Maria Silva", "123"));

            result.ShouldHaveValidationErrorFor(command => command.Document);

            // Só o rótulo é conferido: a frase em volta é da FluentValidation e sai na
            // cultura do processo, porque o validator instanciado aqui não passa pelo
            // AddApplication, que é quem fixa o pt-BR. Quem guarda o idioma da mensagem é
            // o ErrorContractTests, que atravessa a composição real.
            result.Errors.Should().Contain(failure => failure.ErrorMessage.Contains("'CPF'"));
        }

        [Fact]
        public void Deve_aceitar_documento_com_exatamente_11_caracteres()
        {
            var command = new CreatePersonCommand("Maria Silva", DocumentoValido);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
