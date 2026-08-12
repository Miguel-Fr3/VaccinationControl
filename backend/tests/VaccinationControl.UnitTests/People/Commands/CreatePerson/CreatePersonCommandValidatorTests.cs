using FluentAssertions;
using FluentValidation.TestHelper;
using VaccinationControl.Application.People.Commands.CreatePerson;

namespace VaccinationControl.UnitTests.People.Commands.CreatePerson
{
    public class CreatePersonCommandValidatorTests
    {
        // CPF com dígitos verificadores corretos: desde que eles são conferidos, um número
        // qualquer de onze dígitos não serve mais de exemplo válido.
        private const string DocumentoValido = "11144477735";

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

        [Theory]
        [InlineData("abcdefghijk")]    // onze letras
        [InlineData("1234567890 ")]    // dez dígitos e um espaço
        [InlineData("123.456.789")]    // onze caracteres com pontuação
        [InlineData("١٢٣٤٥٦٧٨٩٠١")]    // onze dígitos arábico-índicos
        public void Deve_recusar_documento_que_nao_seja_onze_digitos(string document)
        {
            // O tamanho sozinho aceitava qualquer coisa com onze caracteres. O espaço é o caso
            // que mais enganava: passava na regra e o Trim do handler o gravava com dez dígitos.
            var result = _validator.TestValidate(new CreatePersonCommand("Maria Silva", document));

            result.ShouldHaveValidationErrorFor(command => command.Document);
        }

        [Theory]
        [InlineData("11144477736")]    // último dígito verificador trocado
        [InlineData("12345678901")]    // onze dígitos em sequência, sem conta que feche
        [InlineData("11111111111")]    // dígito repetido: fecha a aritmética e não é CPF
        public void Deve_recusar_documento_com_digito_verificador_errado(string document)
        {
            var result = _validator.TestValidate(new CreatePersonCommand("Maria Silva", document));

            result.ShouldHaveValidationErrorFor(command => command.Document);
        }

        [Fact]
        public void Deve_acusar_uma_falha_por_vez_no_documento()
        {
            // Sem o Cascade.Stop, o campo vazio acusaria as três regras juntas e o formulário
            // mostraria "informe o CPF" ao lado de "CPF não é válido".
            var result = _validator.TestValidate(new CreatePersonCommand("Maria Silva", ""));

            result.Errors.Should().ContainSingle();
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
        public void Deve_aceitar_cpf_valido()
        {
            var command = new CreatePersonCommand("Maria Silva", DocumentoValido);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
