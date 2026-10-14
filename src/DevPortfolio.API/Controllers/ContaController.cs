using DevPortfolio.API.Configuration;
using DevPortfolio.API.Contracts;
using DevPortfolio.Application.Auth.Dtos;
using DevPortfolio.Application.Conta.Dtos;
using DevPortfolio.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DevPortfolio.API.Controllers;

/// <summary>Cadastro de contas e recuperação de senha.</summary>
[ApiController]
[Route("api/conta")]
[AllowAnonymous]
[EnableRateLimiting(SegurancaExtensions.PoliticaConta)]
[Produces("application/json")]
[Consumes("application/json")]
public class ContaController : ControllerBase
{
    private const string MensagemRedefinicaoSolicitada =
        "Se o e-mail estiver cadastrado, você receberá um link para redefinir sua senha.";

    private readonly IContaService _contaService;

    public ContaController(IContaService contaService)
    {
        _contaService = contaService;
    }

    /// <summary>Cria uma conta com perfil Visitante e envia o e-mail de boas-vindas.</summary>
    /// <response code="201">Conta criada.</response>
    /// <response code="400">Dados inválidos ou token anti-CSRF ausente.</response>
    /// <response code="409">E-mail já cadastrado.</response>
    /// <response code="429">Muitas tentativas.</response>
    [HttpPost("registro")]
    [ProducesResponseType(typeof(ApiResposta<UsuarioAutenticadoResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Registrar([FromBody] RegistroRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _contaService.RegistrarAsync(request, cancellationToken);

        if (!resultado.Sucesso)
        {
            return Conflict(ApiResposta.Falha(CodigosErro.EmailJaCadastrado,
                "Este e-mail já está cadastrado. Faça login ou recupere sua senha."));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResposta.Ok(resultado.Usuario!));
    }

    /// <summary>Envia o link de redefinição de senha, se o e-mail estiver cadastrado.</summary>
    /// <remarks>A resposta é a mesma para qualquer e-mail, para não revelar quais contas existem.</remarks>
    /// <response code="202">Solicitação recebida.</response>
    /// <response code="400">Dados inválidos ou token anti-CSRF ausente.</response>
    /// <response code="429">Muitas tentativas.</response>
    [HttpPost("esqueci-senha")]
    [ProducesResponseType(typeof(ApiResposta<string>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest request, CancellationToken cancellationToken)
    {
        await _contaService.SolicitarRedefinicaoSenhaAsync(request, cancellationToken);
        return Accepted(ApiResposta.Ok(MensagemRedefinicaoSolicitada));
    }

    /// <summary>Define a nova senha usando o token recebido por e-mail.</summary>
    /// <response code="204">Senha alterada.</response>
    /// <response code="400">Dados inválidos, token inválido, expirado ou já utilizado.</response>
    /// <response code="429">Muitas tentativas.</response>
    [HttpPost("redefinir-senha")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResposta<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequest request, CancellationToken cancellationToken)
    {
        if (!await _contaService.RedefinirSenhaAsync(request, cancellationToken))
        {
            return BadRequest(ApiResposta.Falha(CodigosErro.TokenInvalido,
                "Este link é inválido ou expirou. Solicite um novo link de redefinição."));
        }

        return NoContent();
    }
}
