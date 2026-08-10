namespace VaccinationControl.Infrastructure.Persistence
{
    /// <summary>
    /// Monta padrões para <c>EF.Functions.Like</c> neutralizando os curingas do termo
    /// buscado: um <c>%</c> ou <c>_</c> digitado pelo usuário é procurado literalmente,
    /// em vez de virar uma expressão de correspondência.
    /// </summary>
    public static class LikePattern
    {
        public const string EscapeCharacter = "\\";

        public static string Contains(string term)
        {
            var escaped = term
                .Trim()
                .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter)
                .Replace("%", EscapeCharacter + "%")
                .Replace("_", EscapeCharacter + "_");

            return $"%{escaped}%";
        }
    }
}
