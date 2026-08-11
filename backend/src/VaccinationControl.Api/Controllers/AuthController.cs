using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinationControl.Api.Security;
using VaccinationControl.Application.Auth;
using VaccinationControl.Application.Auth.Commands.Login;
using VaccinationControl.Application.Auth.Commands.RegisterUser;
using VaccinationControl.Application.Auth.Queries.GetCurrentUser;

namespace VaccinationControl.Api.Controllers
{
    /// <summary>
    /// Abre e encerra a sessão exigida por todos os demais endpoints. O token não aparece em
    /// nenhuma resposta: ele sai daqui em cookie <c>HttpOnly</c> e volta pelo mesmo caminho.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController(ISender sender) : ControllerBase
    {
        /// <summary>
        /// Cadastra um usuário e já abre a sessão.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterUserCommand command,
            CancellationToken cancellationToken)
        {
            var authentication = await sender.Send(command, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, OpenSession(authentication));
        }

        /// <summary>
        /// Autentica com e-mail e senha e abre a sessão.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginCommand command,
            CancellationToken cancellationToken)
        {
            var authentication = await sender.Send(command, cancellationToken);

            return Ok(OpenSession(authentication));
        }

        /// <summary>
        /// Encerra a sessão, apagando o cookie de acesso.
        /// </summary>
        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Logout()
        {
            AuthCookie.Delete(Response);

            return NoContent();
        }

        /// <summary>
        /// Devolve quem está autenticado na sessão corrente.
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var session = await sender.Send(new GetCurrentUserQuery(), cancellationToken);

            return Ok(session);
        }

        /// <summary>
        /// Grava o token no cookie e devolve o que pode ir para o corpo. A separação é o
        /// ponto da mudança: o token fica com o navegador, a identidade vai para a interface.
        /// </summary>
        private SessionResponse OpenSession(AuthenticationResult authentication)
        {
            AuthCookie.Append(Response, authentication.Token, authentication.ExpiresAtUtc);

            return new SessionResponse(authentication.UserId, authentication.Email);
        }
    }
}
