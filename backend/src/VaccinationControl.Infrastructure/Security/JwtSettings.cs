namespace VaccinationControl.Infrastructure.Security
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public int ExpiresInMinutes { get; set; } = 60;

        /// <summary>
        /// Não vive no appsettings — vem de user-secrets em desenvolvimento e de variável de
        /// ambiente em produção. Uma chave commitada fica no histórico do Git para sempre.
        /// </summary>
        public string Key { get; set; } = string.Empty;
    }
}
