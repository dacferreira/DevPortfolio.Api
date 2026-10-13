using DevPortfolio.Application.Interfaces;
using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Enums;
using DevPortfolio.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DevPortfolio.Infrastructure.Persistence;

// Cria o administrador inicial a partir de variáveis de ambiente / user-secrets,
// para que nenhuma senha precise ficar em script SQL ou no appsettings.
public static class UsuarioInicialSeeder
{
    public static async Task ExecutarAsync(IServiceProvider serviceProvider, string nome, string email, string senha,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await repository.ObterPorEmailAsync(email, cancellationToken) is not null)
            return;

        var usuario = new Usuario(nome, email, PerfilUsuario.Administrador);
        usuario.DefinirSenhaHash(hasher.GerarHash(senha));

        await repository.AdicionarAsync(usuario, cancellationToken);
        await repository.SalvarAlteracoesAsync(cancellationToken);
    }
}
