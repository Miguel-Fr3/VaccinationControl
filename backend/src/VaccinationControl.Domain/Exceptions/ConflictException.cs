namespace VaccinationControl.Domain.Exceptions
{
    /// <summary>
    /// A requisição é válida em forma, mas colide com o estado atual — documento já
    /// cadastrado, dose repetida, dose fora de sequência. Traduzida para 409 pela API.
    /// </summary>
    public class ConflictException : DomainException
    {
        public ConflictException(string message)
            : base(message)
        {
        }

        public ConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
