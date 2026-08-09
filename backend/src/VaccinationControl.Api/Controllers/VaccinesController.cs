using MediatR;
using Microsoft.AspNetCore.Mvc;
using VaccinationControl.Application.Common.Models;
using VaccinationControl.Application.Vaccines;
using VaccinationControl.Application.Vaccines.Commands.CreateVaccine;
using VaccinationControl.Application.Vaccines.Queries.GetVaccineById;
using VaccinationControl.Application.Vaccines.Queries.GetVaccines;

namespace VaccinationControl.Api.Controllers
{
    [ApiController]
    [Route("api/vaccines")]
    [Produces("application/json")]
    public class VaccinesController : ControllerBase
    {
        private readonly ISender _sender;

        public VaccinesController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Cadastra uma vacina.
        /// </summary>
        [HttpPost]
        [ProducesResponseType<VaccineResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateVaccineCommand command,
            CancellationToken cancellationToken)
        {
            var vaccine = await _sender.Send(command, cancellationToken);

            // Location resolvido pela própria action de consulta: nada de caminho literal,
            // e o header aponta para um recurso que realmente responde.
            return CreatedAtAction(nameof(GetById), new { id = vaccine.Id }, vaccine);
        }

        /// <summary>
        /// Consulta uma vacina pelo identificador.
        /// </summary>
        /// <param name="id">Identificador da vacina.</param>
        /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
        [HttpGet("{id:guid}")]
        [ProducesResponseType<VaccineResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var vaccine = await _sender.Send(new GetVaccineByIdQuery(id), cancellationToken);

            return Ok(vaccine);
        }

        /// <summary>
        /// Lista as vacinas cadastradas. Sem parâmetros, devolve o catálogo inteiro.
        /// </summary>
        /// <param name="search">Trecho do nome da vacina. Opcional.</param>
        /// <param name="page">Página desejada, a partir de 1. Opcional.</param>
        /// <param name="pageSize">Itens por página, de 1 a 100. Opcional; padrão 20.</param>
        /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
        [HttpGet]
        [ProducesResponseType<PagedResult<VaccineResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var query = new GetVaccinesQuery(search, page, pageSize);

            var vaccines = await _sender.Send(query, cancellationToken);

            return Ok(vaccines);
        }
    }
}
