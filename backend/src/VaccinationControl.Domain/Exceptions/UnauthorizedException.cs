namespace VaccinationControl.Domain.Exceptions
{
    /// <summary>
    /// Credencial ausente ou inválida. Traduzida para 401 pela API.
    /// </summary>
    public class UnauthorizedException : DomainException
    {
        public UnauthorizedException(string message)
            : base(message)
        {
        }
    }
}
