namespace VaccinationControl.Api.Security
{
    public class CorsSettings
    {
        public const string SectionName = "Cors";

        /// <summary>
        /// Nome da política registrada no contêiner e aplicada ao pipeline. Uma constante em vez
        /// de duas strings soltas: erro de digitação entre o registro e o uso não é erro de
        /// compilação, e o sintoma seria a requisição do navegador falhar sem explicação.
        /// </summary>
        public const string PolicyName = "frontend";

        /// <summary>
        /// Origens autorizadas a chamar a API pelo navegador. Precisa ser explícita: a sessão
        /// trafega em cookie, e a especificação proíbe o curinga de <c>AllowAnyOrigin</c> junto
        /// de credenciais — o ASP.NET recusa a combinação em runtime, não na configuração.
        /// Em produção, sobrescreva por variável de ambiente (<c>Cors__AllowedOrigins__0</c>).
        /// </summary>
        public string[] AllowedOrigins { get; set; } = [];
    }
}
