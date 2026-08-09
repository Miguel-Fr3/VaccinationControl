namespace VaccinationControl.Application.Common.Models
{
    /// <summary>
    /// Envelope padrão das listagens. Sempre presente, mesmo quando a requisição não pede
    /// paginação — nesse caso <see cref="PageSize"/> reflete o total devolvido.
    /// </summary>
    public record PagedResult<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalCount)
    {
        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
