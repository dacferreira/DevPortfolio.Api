using System.Security.Claims;
using DevPortfolio.API.Configuration;
using DevPortfolio.API.Contracts;
using DevPortfolio.Application.Auth;
using DevPortfolio.Application.Auth.Dtos;
using DevPortfolio.Application.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace DevPortfolio.API.Controllers;

/// <summary>Autenticação e sessão do usuário.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAntiforgery _antiforgery;
    private readonly SegurancaOptions _seguranca;

    public AuthController(IAuthService authService, IAntiforgery antiforgery, IOptions<SegurancaOptions> seguranca)
    {
        _authService = authService;
        _antiforgery = antiforgery;
        _seguranca = seguranca.Value;
    }

    /// <summary>Emite o token anti-CSRF no cookie XSRF-TOKEN.</summary>
    /// <remarks>Deve ser chamado antes do primeiro POST (ex.: login).</remarks>
    /// <response code="204">Token emitido.</response>
    [HttpGet("csrf")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ObterTokenCsrf()
    {
        EmitirTokenCsrf();
        return NoContent();
    }

    /// <summary>Autentica o usuário e inicia a sessão (cookie HttpOnly).</summary>
    /// <response code="200">Login realizado.</response>
    /// <response code="400">Dados inválidos ou token anti-CSRF ausente.</response>
    /// <response code="401">E-mail ou senha inválidos.</response>
    /// <response code="403">Usuário inativo.</response>
    /// <response code="429">Muitas tentativas de login.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(SegurancaExtensions.PoliticaLogin)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResposta<UsuarioAutenticadoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _authService.AutenticarAsync(request, cancellationToken);

        if (!resultado.Sucesso)
        {
            return resultado.Falha == FalhaAutenticacao.UsuarioInativo
                ? StatusCode(StatusCodes.Status403Forbidden, ApiResposta.Falha(CodigosErro.UsuarioInativo,
                    "Usuário inativo. Entre em contato com o administrador."))
                : Unauthorized(ApiResposta.Falha(CodigosErro.CredenciaisInvalidas, "E-mail ou senha inválidos."));
        }

        var usuario = resultado.Usuario!;
        var principal = CriarPrincipal(usuario);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = request.LembrarDeMim });

        // O token anti-CSRF é vinculado ao usuário: renova para a nova identidade.
        HttpContext.User = principal;
        EmitirTokenCsrf();

        return Ok(ApiResposta.Ok(usuario));
    }

    /// <summary>Encerra a sessão atual.</summary>
    /// <response code="204">Sessão encerrada.</response>
    /// <response code="401">Não autenticado.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await EncerrarSessaoAsync();
        return NoContent();
    }

    /// <summary>Retorna o usuário da sessão atual.</summary>
    /// <response code="200">Usuário autenticado.</response>
    /// <response code="401">Sessão ausente, expirada ou usuário inativo.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResposta<UsuarioAutenticadoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObterUsuarioAtual(CancellationToken cancellationToken)
    {
        var usuario = long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? await _authService.ObterUsuarioAtivoAsync(id, cancellationToken)
            : null;

        // Usuário removido ou inativado depois do login perde a sessão.
        if (usuario is null)
        {
            await EncerrarSessaoAsync();
            return Unauthorized(ApiResposta.Falha(CodigosErro.NaoAutenticado, "Faça login para continuar."));
        }

        return Ok(ApiResposta.Ok(usuario));
    }

    private async Task EncerrarSessaoAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        EmitirTokenCsrf();
    }

    private void EmitirTokenCsrf()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        Response.Cookies.Append(SegurancaExtensions.CookieTokenCsrf, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = _seguranca.ExigirHttps || Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
    }

    private static ClaimsPrincipal CriarPrincipal(UsuarioAutenticadoResponse usuario)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Perfil),
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
