using DevPortfolio.Application.Interfaces;
using DevPortfolio.Domain.Interfaces;
using DevPortfolio.Infrastructure.Persistence;
using DevPortfolio.Infrastructure.Repositories;
using DevPortfolio.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevPortfolio.Infrastructure;

public static class DependencyInjection
{
    // Versão fixa evita que o Pomelo abra uma conexão na inicialização só para detectá-la.
    private static readonly MySqlServerVersion VersaoMySql = new(new Version(8, 0, 36));

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DevPortfolioDbContext>(options =>
            options.UseMySql(connectionString, VersaoMySql, mysql => mysql.EnableRetryOnFailure()));

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ITokenRedefinicaoSenhaRepository, TokenRedefinicaoSenhaRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
