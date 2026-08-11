namespace VaccinationControl.Api.Security
{
    /// <summary>
    /// O cookie que carrega o token de acesso. É <c>HttpOnly</c> porque essa é a razão de existir:
    /// um script injetado por XSS não consegue lê-lo, ao contrário de um token guardado em
    /// <c>localStorage</c>. Em troca, o navegador passa a enviá-lo sozinho — daí o
    /// <c>SameSite=Lax</c>, que bloqueia o envio em requisição partida de outro site.
    /// </summary>
    public static class AuthCookie
    {
        /// <summary>
        /// Mesma constante usada para gravar, apagar e ler o token no <c>AddJwtBearer</c>. Três
        /// literais soltas não seriam erro de compilação, e o sintoma de uma divergência seria
        /// a API ignorar em silêncio o cookie que ela própria acabou de emitir.
        /// </summary>
        public const string Name = "vaccination-control-auth";

        public static void Append(HttpResponse response, string token, DateTime expiresAtUtc)
        {
            // A validade acompanha a do token: vencendo junto, a sessão morre nos dois lados ao
            // mesmo tempo, sem o intervalo em que o navegador ainda manda um token já recusado.
            response.Cookies.Append(Name, token, CreateOptions(expiresAtUtc));
        }

        public static void Delete(HttpResponse response)
        {
            // Apagar exige as mesmas marcas da gravação: se Path, Secure ou SameSite divergirem,
            // o navegador entende que é outro cookie e o original sobrevive ao logout.
            response.Cookies.Delete(Name, CreateOptions(DateTime.UnixEpoch));
        }

        private static CookieOptions CreateOptions(DateTime expiresAtUtc)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                // Vale mesmo em desenvolvimento: navegadores tratam localhost como contexto
                // seguro, então o cookie é aceito em HTTP sem abrir exceção na configuração.
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = expiresAtUtc
            };
        }
    }
}
