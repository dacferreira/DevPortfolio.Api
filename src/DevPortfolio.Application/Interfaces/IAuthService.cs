using DevPortfolio.Application.Auth;
using DevPortfolio.Application.Auth.Dtos;

namespace DevPortfolio.Application.Interfaces;

public interface IAuthService
{
    Task<ResultadoAutenticacao> AutenticarAsync(LoginRequest request, CancellationToken cancellationToken = default);

    // Retorna o usuário apenas se ele ainda existir e estiver ativo.
    Task<UsuarioAutenticadoResponse?> ObterUsuarioAtivoAsync(long id, CancellationToken cancellationToken = default);
}
