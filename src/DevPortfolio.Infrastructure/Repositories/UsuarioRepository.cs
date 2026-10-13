using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Interfaces;
using DevPortfolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevPortfolio.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly DevPortfolioDbContext _context;

    public UsuarioRepository(DevPortfolioDbContext context)
    {
        _context = context;
    }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalizado = Usuario.NormalizarEmail(email);
        return _context.Usuarios.FirstOrDefaultAsync(u => u.Email == emailNormalizado, cancellationToken);
    }

    public Task<Usuario?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
        await _context.Usuarios.AddAsync(usuario, cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
