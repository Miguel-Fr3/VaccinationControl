using FluentAssertions;
using VaccinationControl.Application.Common.Validation;

namespace VaccinationControl.UnitTests.Common.Validation
{
    public class CpfTests
    {
        [Theory]
        [InlineData("11144477735")]
        [InlineData("52998224725")]
        [InlineData("01234567890")]   // começa com zero: o dígito da frente não pode ser perdido
        public void Deve_aceitar_cpf_com_digitos_verificadores_corretos(string document)
        {
            Cpf.IsValid(document).Should().BeTrue();
        }

        [Theory]
        [InlineData("11144477736")]   // último dígito trocado
        [InlineData("11144477725")]   // penúltimo dígito trocado
        [InlineData("11444777350")]   // dígitos certos, na ordem errada
        public void Deve_recusar_cpf_com_digitos_verificadores_errados(string document)
        {
            Cpf.IsValid(document).Should().BeFalse();
        }

        [Theory]
        [InlineData("00000000000")]
        [InlineData("11111111111")]
        [InlineData("99999999999")]
        public void Deve_recusar_sequencia_de_digito_repetido(string document)
        {
            // Elas fecham a aritmética dos verificadores e não são CPF de ninguém: é o caso
            // que o cálculo sozinho deixaria passar.
            Cpf.IsValid(document).Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1114447773")]    // dez dígitos
        [InlineData("111444777350")]  // doze dígitos
        [InlineData("111.444.777-35")]
        [InlineData("abcdefghijk")]
        public void Deve_recusar_entrada_que_nao_seja_onze_digitos(string? document)
        {
            // O validator já barra estes antes de chegar aqui, mas o método precisa ser
            // seguro sozinho — nada garante que o próximo chamador filtre a entrada.
            Cpf.IsValid(document).Should().BeFalse();
        }
    }
}
