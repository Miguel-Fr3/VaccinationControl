namespace VaccinationControl.Application.Common.Interfaces
{
    /// <summary>
    /// Identidade de quem está fazendo a requisição, extraída do token. É o que alimenta
    /// os campos de auditoria (<c>CreatedBy</c> e <c>UpdatedBy</c>) de toda entidade gravada.
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// Nulo em requisições anônimas — login e cadastro de usuário.
        /// </summary>
        Guid? Id { get; }
    }
}
