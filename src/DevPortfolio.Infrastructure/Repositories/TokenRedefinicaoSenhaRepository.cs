using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Interfaces;
using DevPortfolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevPortfolio.Infrastructure.Repositories;

// Usa o mesmo DbContext do UsuarioRepository: SalvarAlteracoesAsync de lá grava os dois.
public class TokenRedefinicaoSenhaRepository : ITokenRedefinicaoSenhaRepository
{
    private readonly DevPortfolioDbContext _context;

    public TokenRedefinicaoSenhaRepository(DevPortfolioDbContext context)
    {
        _context = context;
    }

    public Task<TokenRedefinicaoSenha?> ObterPorHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.TokensRedefinicaoSenha.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<TokenRedefinicaoSenha>> ObterPendentesDoUsuarioAsync(long usuarioId,
        CancellationToken cancellationToken = default) =>
        await _context.TokensRedefinicaoSenha
            .Where(t => t.UsuarioId == usuarioId && t.DataUtilizacao == null)
            .ToListAsync(cancellationToken);

    public async Task AdicionarAsync(TokenRedefinicaoSenha token, CancellationToken cancellationToken = default) =>
        await _context.TokensRedefinicaoSenha.AddAsync(token, cancellationToken);
}
