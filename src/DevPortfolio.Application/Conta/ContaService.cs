using DevPortfolio.Application.Auth.Dtos;
using DevPortfolio.Application.Conta.Dtos;
using DevPortfolio.Application.Interfaces;
using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Enums;
using DevPortfolio.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace DevPortfolio.Application.Conta;

public class ContaService : IContaService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenRedefinicaoSenhaRepository _tokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly TimeProvider _relogio;
    private readonly ContaOptions _opcoes;

    public ContaService(
        IUsuarioRepository usuarioRepository,
        ITokenRedefinicaoSenhaRepository tokenRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        TimeProvider relogio,
        IOptions<ContaOptions> opcoes)
    {
        _usuarioRepository = usuarioRepository;
        _tokenRepository = tokenRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _relogio = relogio;
        _opcoes = opcoes.Value;
    }

    public async Task<ResultadoRegistro> RegistrarAsync(RegistroRequest request, CancellationToken cancellationToken = default)
    {
        if (await _usuarioRepository.ObterPorEmailAsync(request.Email, cancellationToken) is not null)
            return ResultadoRegistro.EmailJaCadastrado();

        // O cadastro público nunca cria administradores.
        var usuario = new Usuario(request.Nome, request.Email, PerfilUsuario.Visitante);
        usuario.DefinirSenhaHash(_passwordHasher.GerarHash(request.Senha));

        await _usuarioRepository.AdicionarAsync(usuario, cancellationToken);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        await _emailService.EnviarBoasVindasAsync(usuario.Nome, usuario.Email, cancellationToken);

        return ResultadoRegistro.Ok(UsuarioAutenticadoResponse.De(usuario));
    }

    public async Task SolicitarRedefinicaoSenhaAsync(EsqueciSenhaRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorEmailAsync(request.Email, cancellationToken);
        if (usuario is not { Ativo: true })
            return;

        var agora = _relogio.GetUtcNow().UtcDateTime;

        // Só o link mais recente vale.
        foreach (var pendente in await _tokenRepository.ObterPendentesDoUsuarioAsync(usuario.Id, cancellationToken))
            pendente.MarcarComoUtilizado(agora);

        var token = TokenSeguro.Gerar();
        var expiracao = agora.AddMinutes(_opcoes.ExpiracaoTokenRedefinicaoMinutos);
        await _tokenRepository.AdicionarAsync(
            new TokenRedefinicaoSenha(usuario.Id, TokenSeguro.CalcularHash(token), expiracao, agora), cancellationToken);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        await _emailService.EnviarRedefinicaoSenhaAsync(usuario.Nome, usuario.Email, token,
            _opcoes.ExpiracaoTokenRedefinicaoMinutos, cancellationToken);
    }

    public async Task<bool> RedefinirSenhaAsync(RedefinirSenhaRequest request, CancellationToken cancellationToken = default)
    {
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var token = await _tokenRepository.ObterPorHashAsync(TokenSeguro.CalcularHash(request.Token), cancellationToken);
        if (token is null || !token.EstaValido(agora))
            return false;

        var usuario = await _usuarioRepository.ObterPorIdAsync(token.UsuarioId, cancellationToken);
        if (usuario is not { Ativo: true })
            return false;

        usuario.DefinirSenhaHash(_passwordHasher.GerarHash(request.NovaSenha));

        // Invalida este e qualquer outro link pendente do usuário.
        foreach (var pendente in await _tokenRepository.ObterPendentesDoUsuarioAsync(usuario.Id, cancellationToken))
            pendente.MarcarComoUtilizado(agora);
        token.MarcarComoUtilizado(agora);

        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
        return true;
    }
}
