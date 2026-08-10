using MediatR;
using Microsoft.AspNetCore.Mvc;
using VaccinationControl.Application.Common.Models;
using VaccinationControl.Application.People;
using VaccinationControl.Application.People.Commands.CreatePerson;
using VaccinationControl.Application.People.Commands.DeletePerson;
using VaccinationControl.Application.People.Queries.GetPeople;
using VaccinationControl.Application.People.Queries.GetPersonById;

namespace VaccinationControl.Api.Controllers
{
    [ApiController]
    [Route("api/people")]
    [Produces("application/json")]
    public class PeopleController(ISender sender) : ControllerBase
    {
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
            var person = await sender.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = person.Id }, person);
        }

        /// <summary>
        /// Lista as pessoas cadastradas. Sem parâmetros, devolve todas.
        /// </summary>
        /// <param name="search">Trecho do nome ou do documento. Opcional.</param>
        /// <param name="page">Página desejada, a partir de 1. Opcional.</param>
        /// <param name="pageSize">Itens por página, de 1 a 100. Opcional; padrão 20.</param>
        /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
        [HttpGet]
        [ProducesResponseType<PagedResult<PersonResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var query = new GetPeopleQuery(search, page, pageSize);

            var people = await sender.Send(query, cancellationToken);

            return Ok(people);
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
            var person = await sender.Send(new GetPersonByIdQuery(id), cancellationToken);

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
            await sender.Send(new DeletePersonCommand(id), cancellationToken);

            return NoContent();
        }
    }
}
