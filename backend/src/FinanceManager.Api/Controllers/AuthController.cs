using System.Security.Claims;
using FinanceManager.Api.Contracts.Requests.Auth;
using FinanceManager.Api.Contracts.Responses.Auth;
using FinanceManager.Application.Authentication;
using FinanceManager.Application.Authentication.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Controllers
{

    [ApiController]
    [Route("api/v1/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(
                new RegisterUserInput(request.FullName, request.Email, request.Password),
                cancellationToken);

            return Ok(MapResponse(result));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(
                new LoginInput(request.Email, request.Password),
                cancellationToken);

            return Ok(MapResponse(result));
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AuthenticatedUserResponse>> Me(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(ClaimTypes.Sid)
                ?? User.FindFirstValue("sub");

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
            return Ok(new AuthenticatedUserResponse(user.Id, user.FullName, user.Email));
        }

        private static AuthResponse MapResponse(AuthResultDto result)
        {
            return new AuthResponse(
                result.AccessToken,
                result.ExpiresAtUtc,
                new AuthenticatedUserResponse(result.User.Id, result.User.FullName, result.User.Email));
        }
    }
}
