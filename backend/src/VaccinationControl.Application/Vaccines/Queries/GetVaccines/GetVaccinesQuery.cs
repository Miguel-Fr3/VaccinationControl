using MediatR;
using VaccinationControl.Application.Common.Models;

namespace VaccinationControl.Application.Vaccines.Queries.GetVaccines
{
    /// <summary>
    /// Todos os parâmetros são opcionais: sem nenhum deles a consulta devolve o catálogo
    /// inteiro. Informar <paramref name="Page"/> ou <paramref name="PageSize"/> ativa a
    /// paginação; o que faltar assume o padrão.
    /// </summary>
    public record GetVaccinesQuery(
        string? Search = null,
        int? Page = null,
        int? PageSize = null) : IRequest<PagedResult<VaccineResponse>>;
}
