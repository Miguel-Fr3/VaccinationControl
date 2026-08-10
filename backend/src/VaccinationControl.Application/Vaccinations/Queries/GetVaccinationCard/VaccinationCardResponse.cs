namespace VaccinationControl.Application.Vaccinations.Queries.GetVaccinationCard
{
    /// <summary>
    /// O cartão de vacinação de uma pessoa. Não existe como tabela: é a projeção dos
    /// registros de vacinação dela, agrupados por vacina.
    /// </summary>
    public record VaccinationCardResponse(
        Guid PersonId,
        string PersonName,
        string Document,
        IReadOnlyList<VaccinationCardVaccineResponse> Vaccines);
}
