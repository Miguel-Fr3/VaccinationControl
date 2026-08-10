namespace VaccinationControl.Application.Auth
{
    /// <summary>
    /// Token de acesso emitido no login. O <c>userId</c> é o mesmo que será gravado em
    /// <c>CreatedBy</c> e <c>UpdatedBy</c> das entidades manipuladas por este usuário.
    /// </summary>
    public record AuthenticationResponse(
        Guid UserId,
        string Email,
        string Token,
        DateTime ExpiresAtUtc);
}
