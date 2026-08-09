namespace VaccinationControl.Domain.Exceptions
{
    /// <summary>
    /// Recurso referenciado pela requisição não existe. Traduzida para 404 pela API.
    /// </summary>
    public class NotFoundException : DomainException
    {
        public NotFoundException(string entityName, object key)
            : base($"Nenhum registro de {entityName} foi encontrado para o identificador '{key}'.")
        {
            EntityName = entityName;
            Key = key;
        }

        public NotFoundException(string message)
            : base(message)
        {
        }

        public string? EntityName { get; }

        public object? Key { get; }
    }
}
