using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Interfaces;

namespace DevPortfolio.Application.Test.Fakes;

public class TokenRedefinicaoSenhaRepositoryFake : ITokenRedefinicaoSenhaRepository
{
    public List<TokenRedefinicaoSenha> Tokens { get; } = new();

    public Task<TokenRedefinicaoSenha?> ObterPorHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task<IReadOnlyList<TokenRedefinicaoSenha>> ObterPendentesDoUsuarioAsync(long usuarioId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TokenRedefinicaoSenha>>(
            Tokens.Where(t => t.UsuarioId == usuarioId && t.DataUtilizacao is null).ToList());

    public Task AdicionarAsync(TokenRedefinicaoSenha token, CancellationToken cancellationToken = default)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }
}
