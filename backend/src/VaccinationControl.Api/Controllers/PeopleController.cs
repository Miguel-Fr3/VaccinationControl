using MediatR;
using Microsoft.AspNetCore.Mvc;
using VaccinationControl.Application.People;
using VaccinationControl.Application.People.Commands.CreatePerson;
using VaccinationControl.Application.People.Commands.DeletePerson;
using VaccinationControl.Application.People.Queries.GetPersonById;

namespace VaccinationControl.Api.Controllers
{
    [ApiController]
    [Route("api/people")]
    [Produces("application/json")]
    public class PeopleController : ControllerBase
    {
        private readonly ISender _sender;

        public PeopleController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Cadastra uma pessoa.
        /// </summary>
        [HttpPost]
        [ProducesResponseType<PersonResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreatePersonCommand command,
            CancellationToken cancellationToken)
        {
            var person = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = person.Id }, person);
        }

        /// <summary>
        /// Consulta uma pessoa pelo identificador.
        /// </summary>
        /// <param name="id">Identificador da pessoa.</param>
        /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
        [HttpGet("{id:guid}")]
        [ProducesResponseType<PersonResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var person = await _sender.Send(new GetPersonByIdQuery(id), cancellationToken);

            return Ok(person);
        }

        /// <summary>
        /// Remove uma pessoa e, junto com ela, todo o seu cartão de vacinação.
        /// </summary>
        /// <param name="id">Identificador da pessoa.</param>
        /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeletePersonCommand(id), cancellationToken);

            return NoContent();
        }
    }
}
