namespace VaccinationControl.Application.Vaccines
{
    /// <summary>
    /// Representação de uma vacina no contrato da API. Compartilhada pelo cadastro e pela
    /// listagem para que as duas rotas nunca divirjam no formato.
    /// </summary>
    public record VaccineResponse(Guid Id, string Name);
}
