namespace VaccinationControl.Application.Common.Validation
{
    /// <summary>
    /// Conferência dos dígitos verificadores do CPF. Fica aqui, e não numa regra solta dentro
    /// do validator, porque o cálculo é longo o bastante para merecer teste próprio — e porque
    /// o dia em que outro caso de uso pedir um CPF ele não deve ser reescrito.
    /// </summary>
    public static class Cpf
    {
        private const int Length = 11;

        /// <summary>
        /// Informa se os onze dígitos formam um CPF possível. Confere apenas a aritmética dos
        /// dois últimos dígitos: um CPF válido aqui pode nunca ter sido emitido, porque isso
        /// só a Receita Federal sabe.
        /// </summary>
        public static bool IsValid(string? document)
        {
            // Repete o formato que o validator já exige: o método precisa ser seguro sozinho,
            // já que nada garante que o chamador tenha filtrado a entrada antes.
            if (document is null || document.Length != Length)
            {
                return false;
            }

            foreach (var character in document)
            {
                if (!char.IsAsciiDigit(character))
                {
                    return false;
                }
            }

            // Sequências de um dígito só passam na aritmética — 111.111.111-11 fecha a conta
            // certinho — e não são CPF de ninguém. É o caso que o cálculo sozinho não pega.
            if (document.All(digit => digit == document[0]))
            {
                return false;
            }

            return CheckDigit(document, 9) == document[9]
                && CheckDigit(document, 10) == document[10];
        }

        /// <summary>
        /// Calcula o dígito verificador dos <paramref name="length"/> primeiros dígitos. Os
        /// pesos descem de <c>length + 1</c> até 2, e o resto da divisão por 11 vira zero
        /// quando é menor que 2.
        /// </summary>
        private static char CheckDigit(string document, int length)
        {
            var sum = 0;

            for (var position = 0; position < length; position++)
            {
                sum += (document[position] - '0') * (length + 1 - position);
            }

            var remainder = sum % 11;
            var digit = remainder < 2 ? 0 : 11 - remainder;

            return (char)(digit + '0');
        }
    }
}
