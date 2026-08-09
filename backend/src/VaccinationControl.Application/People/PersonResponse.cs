namespace VaccinationControl.Application.People
{
    /// <summary>
    /// Representação de uma pessoa no contrato da API, compartilhada pelas rotas da feature.
    /// </summary>
    public record PersonResponse(Guid Id, string Name, string Document);
}
