using VaccinationControl.Application.Common.Validation;

namespace VaccinationControl.IntegrationTests.Support
{
    /// <summary>
    /// Produz um CPF diferente a cada chamada. Os cenários precisam de documento novo porque o
    /// índice único recusa repetição, e sortear onze dígitos quaisquer deixou de servir desde
    /// que os verificadores são conferidos — noventa e nove por cento dos sorteios voltariam 400.
    /// </summary>
    public static class CpfGenerator
    {
        /// <summary>
        /// Nove dígitos sorteados e os dois verificadores calculados por cima deles. Se a conta
        /// aqui estiver errada, todo cenário que cadastra pessoa falha com 400 na primeira
        /// requisição — o erro não tem como passar despercebido.
        /// </summary>
        public static string Next()
        {
            var digits = new char[11];

            for (var position = 0; position < 9; position++)
            {
                digits[position] = (char)('0' + Random.Shared.Next(10));
            }

            digits[9] = CheckDigit(digits, 9);
            digits[10] = CheckDigit(digits, 10);

            var document = new string(digits);

            // Sequência de dígito repetido é o único sorteio que a validação recusa mesmo com
            // os verificadores certos. É raro, e a saída é sortear de novo.
            return Cpf.IsValid(document) ? document : Next();
        }

        private static char CheckDigit(char[] digits, int length)
        {
            var sum = 0;

            for (var position = 0; position < length; position++)
            {
                sum += (digits[position] - '0') * (length + 1 - position);
            }

            var remainder = sum % 11;

            return (char)((remainder < 2 ? 0 : 11 - remainder) + '0');
        }
    }
}
