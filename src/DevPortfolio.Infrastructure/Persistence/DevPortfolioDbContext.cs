using DevPortfolio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevPortfolio.Infrastructure.Persistence;

// O schema é versionado pelo Flyway (DevPortfolio.Dados); o EF Core apenas mapeia as tabelas.
public class DevPortfolioDbContext : DbContext
{
    public DevPortfolioDbContext(DbContextOptions<DevPortfolioDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DevPortfolioDbContext).Assembly);
    }
}
