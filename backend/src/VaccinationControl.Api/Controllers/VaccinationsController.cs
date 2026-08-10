using MediatR;
using Microsoft.AspNetCore.Mvc;
using VaccinationControl.Application.Vaccinations;
using VaccinationControl.Application.Vaccinations.Commands.DeleteVaccinationRecord;
using VaccinationControl.Application.Vaccinations.Commands.RegisterVaccination;
using VaccinationControl.Application.Vaccinations.Queries.GetVaccinationRecordById;

namespace VaccinationControl.Api.Controllers
{
    [ApiController]
    [Route("api/people/{personId:guid}/vaccinations")]
    [Produces("application/json")]
    public class VaccinationsController(ISender sender) : ControllerBase
    {
        /// <summary>
        /// Registra uma vacinação no cartão de uma pessoa.
        /// </summary>
        /// <param name="personId">Identificador da pessoa vacinada.</param>
        /// <param name="request">Vacina, tipo, número da dose e data de aplicação.</param>
        /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
        [HttpPost]
        [ProducesResponseType<VaccinationRecordResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            Guid personId,
            [FromBody] RegisterVaccinationRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RegisterVaccinationCommand(
                personId,
                request.VaccineId,
                request.VaccinationType,
                request.DoseNumber,
                request.VaccinationDate);

            var record = await sender.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { personId, recordId = record.Id },
                record);
        }

        /// <summary>
        /// Consulta um registro de vacinação do cartão de uma pessoa.
        /// </summary>
        /// <param name="personId">Identificador da pessoa.</param>
        /// <param name="recordId">Identificador do registro de vacinação.</param>
        /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
        [HttpGet("{recordId:guid}")]
        [ProducesResponseType<VaccinationRecordResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            Guid personId,
            Guid recordId,
            CancellationToken cancellationToken)
        {
            var query = new GetVaccinationRecordByIdQuery(personId, recordId);

            var record = await sender.Send(query, cancellationToken);

            return Ok(record);
        }

        /// <summary>
        /// Remove um registro de vacinação do cartão de uma pessoa.
        /// </summary>
        /// <param name="personId">Identificador da pessoa.</param>
        /// <param name="recordId">Identificador do registro de vacinação.</param>
        /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
        [HttpDelete("{recordId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            Guid personId,
            Guid recordId,
            CancellationToken cancellationToken)
        {
            var command = new DeleteVaccinationRecordCommand(personId, recordId);

            await sender.Send(command, cancellationToken);

            return NoContent();
        }
    }
}
