namespace VaccinationControl.Domain.Exceptions
{
    /// <summary>
    /// Base das falhas previstas pelas regras de negócio. Serve para distinguir o que o
    /// domínio recusou de propósito de uma falha inesperada, que deve continuar virando 500.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message)
            : base(message)
        {
        }

        public DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
