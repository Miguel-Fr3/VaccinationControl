using MediatR;
using VaccinationControl.Application.Common.Models;

namespace VaccinationControl.Application.People.Queries.GetPeople
{
    /// <summary>
    /// Todos os parâmetros são opcionais: sem nenhum deles a consulta devolve todas as
    /// pessoas. Informar <paramref name="Page"/> ou <paramref name="PageSize"/> ativa a
    /// paginação; o que faltar assume o padrão.
    /// </summary>
    public record GetPeopleQuery(
        string? Search = null,
        int? Page = null,
        int? PageSize = null) : IRequest<PagedResult<PersonResponse>>;
}
