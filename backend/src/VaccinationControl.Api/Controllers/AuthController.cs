using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinationControl.Application.Auth;
using VaccinationControl.Application.Auth.Commands.Login;
using VaccinationControl.Application.Auth.Commands.RegisterUser;

namespace VaccinationControl.Api.Controllers
{
    /// <summary>
    /// Único controller anônimo da API: é por aqui que se obtém o token exigido por
    /// todos os demais endpoints.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class AuthController(ISender sender) : ControllerBase
    {
        /// <summary>
        /// Cadastra um usuário e já devolve o token de acesso.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterUserCommand command,
            CancellationToken cancellationToken)
        {
            var authentication = await sender.Send(command, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, authentication);
        }

        /// <summary>
        /// Autentica com e-mail e senha e devolve o token de acesso.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginCommand command,
            CancellationToken cancellationToken)
        {
            var authentication = await sender.Send(command, cancellationToken);

            return Ok(authentication);
        }
    }
}
