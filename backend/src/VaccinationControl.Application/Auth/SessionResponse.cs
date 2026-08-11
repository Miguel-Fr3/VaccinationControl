namespace VaccinationControl.Application.Auth
{
    /// <summary>
    /// Quem está autenticado na sessão corrente. É o corpo devolvido pelo cadastro, pelo login
    /// e pela consulta de sessão — a interface não consegue ler o cookie para descobrir sozinha.
    /// </summary>
    public record SessionResponse(Guid UserId, string Email);
}
