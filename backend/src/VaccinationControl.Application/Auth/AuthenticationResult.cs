namespace VaccinationControl.Application.Auth
{
    /// <summary>
    /// Resultado de uma autenticação bem-sucedida. Não é o corpo da resposta: o token sai da
    /// Api em cookie <c>HttpOnly</c>, e devolvê-lo também em JSON anularia o ganho — o
    /// JavaScript da interface voltaria a ter acesso a ele.
    /// </summary>
    /// <remarks>
    /// O <c>UserId</c> é o mesmo que será gravado em <c>CreatedBy</c> e <c>UpdatedBy</c> das
    /// entidades manipuladas por este usuário.
    /// </remarks>
    public record AuthenticationResult(
        Guid UserId,
        string Email,
        string Token,
        DateTime ExpiresAtUtc);
}
