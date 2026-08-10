using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Application.Common.Models;

namespace VaccinationControl.Application.Vaccines.Queries.GetVaccines
{
    public class GetVaccinesQueryHandler(IVaccineRepository vaccineRepository)
        : IRequestHandler<GetVaccinesQuery, PagedResult<VaccineResponse>>
    {
        private const int DefaultPage = 1;
        private const int DefaultPageSize = 20;

        public async Task<PagedResult<VaccineResponse>> Handle(
            GetVaccinesQuery request,
            CancellationToken cancellationToken)
        {
            var isPaginated = request.Page.HasValue || request.PageSize.HasValue;

            var page = request.Page ?? DefaultPage;
            var pageSize = request.PageSize ?? DefaultPageSize;

            int? skip = isPaginated ? (page - 1) * pageSize : null;
            int? take = isPaginated ? pageSize : null;

            var (vaccines, totalCount) = await vaccineRepository.SearchAsync(
                request.Search,
                skip,
                take,
                cancellationToken);

            var items = vaccines
                .Select(vaccine => new VaccineResponse(vaccine.Id, vaccine.Name))
                .ToList();

            return new PagedResult<VaccineResponse>(
                items,
                isPaginated ? page : DefaultPage,
                // Sem paginação a "página" é o resultado inteiro.
                isPaginated ? pageSize : totalCount,
                totalCount);
        }
    }
}
