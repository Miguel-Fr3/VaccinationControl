using MediatR;
using Microsoft.AspNetCore.Mvc;
using VaccinationControl.Application.Vaccinations.Queries.GetVaccinationCard;

namespace VaccinationControl.Api.Controllers
{
    [ApiController]
    [Route("api/people/{personId:guid}/vaccination-card")]
    [Produces("application/json")]
    public class VaccinationCardsController : ControllerBase
    {
        private readonly ISender _sender;

        public VaccinationCardsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Consulta o cartão de vacinação de uma pessoa, com as aplicações agrupadas por vacina.
        /// </summary>
        /// <param name="personId">Identificador da pessoa.</param>
        /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
        [HttpGet]
        [ProducesResponseType<VaccinationCardResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByPerson(
            Guid personId,
            CancellationToken cancellationToken)
        {
            var card = await _sender.Send(new GetVaccinationCardQuery(personId), cancellationToken);

            return Ok(card);
        }
    }
}
