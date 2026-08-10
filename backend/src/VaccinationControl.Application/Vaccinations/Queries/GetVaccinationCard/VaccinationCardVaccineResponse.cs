namespace VaccinationControl.Application.Vaccinations.Queries.GetVaccinationCard
{
    /// <summary>
    /// As aplicações agrupadas por vacina — o enunciado pede o cartão com o nome da vacina
    /// e as doses recebidas dela, não uma lista plana de registros soltos.
    /// </summary>
    public record VaccinationCardVaccineResponse(
        Guid VaccineId,
        string VaccineName,
        int TotalDoses,
        IReadOnlyList<VaccinationCardDoseResponse> Doses);
}
