using DevPortfolio.Domain.Entities;

namespace DevPortfolio.Domain.Interfaces;

public interface ITokenRedefinicaoSenhaRepository
{
    Task<TokenRedefinicaoSenha?> ObterPorHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    // Tokens ainda não utilizados do usuário (para invalidá-los ao gerar um novo ou ao trocar a senha).
    Task<IReadOnlyList<TokenRedefinicaoSenha>> ObterPendentesDoUsuarioAsync(long usuarioId, CancellationToken cancellationToken = default);

    Task AdicionarAsync(TokenRedefinicaoSenha token, CancellationToken cancellationToken = default);
}
