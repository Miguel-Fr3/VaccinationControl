using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Application.Common.Models;

namespace VaccinationControl.Application.People.Queries.GetPeople
{
    public class GetPeopleQueryHandler(IPersonRepository personRepository) : IRequestHandler<GetPeopleQuery, PagedResult<PersonResponse>>
    {
        private const int DefaultPage = 1;
        private const int DefaultPageSize = 20;

        public async Task<PagedResult<PersonResponse>> Handle(
            GetPeopleQuery request,
            CancellationToken cancellationToken)
        {
            var isPaginated = request.Page.HasValue || request.PageSize.HasValue;

            var page = request.Page ?? DefaultPage;
            var pageSize = request.PageSize ?? DefaultPageSize;

            int? skip = isPaginated ? (page - 1) * pageSize : null;
            int? take = isPaginated ? pageSize : null;

            var (people, totalCount) = await personRepository.SearchAsync(
                request.Search,
                skip,
                take,
                cancellationToken);

            var items = people
                .Select(person => new PersonResponse(person.Id, person.Name, person.Document))
                .ToList();

            return new PagedResult<PersonResponse>(
                items,
                isPaginated ? page : DefaultPage,
                // Sem paginação a "página" é o resultado inteiro.
                isPaginated ? pageSize : totalCount,
                totalCount);
        }
    }
}
