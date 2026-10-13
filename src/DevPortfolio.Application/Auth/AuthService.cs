using DevPortfolio.Application.Auth.Dtos;
using DevPortfolio.Application.Interfaces;
using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Interfaces;

namespace DevPortfolio.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ResultadoAutenticacao> AutenticarAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = Usuario.NormalizarEmail(request.Email);
        var usuario = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);

        if (usuario is null)
        {
            _passwordHasher.SimularVerificacao(request.Senha);
            return ResultadoAutenticacao.Falhou(FalhaAutenticacao.CredenciaisInvalidas);
        }

        if (!_passwordHasher.Verificar(usuario.SenhaHash, request.Senha))
            return ResultadoAutenticacao.Falhou(FalhaAutenticacao.CredenciaisInvalidas);

        // O status da conta só é informado depois que a senha confere.
        if (!usuario.Ativo)
            return ResultadoAutenticacao.Falhou(FalhaAutenticacao.UsuarioInativo);

        usuario.RegistrarAcesso();
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        return ResultadoAutenticacao.Ok(UsuarioAutenticadoResponse.De(usuario));
    }

    public async Task<UsuarioAutenticadoResponse?> ObterUsuarioAtivoAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id, cancellationToken);
        return usuario is { Ativo: true } ? UsuarioAutenticadoResponse.De(usuario) : null;
    }
}
