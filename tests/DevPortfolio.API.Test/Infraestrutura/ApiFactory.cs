using DevPortfolio.API.Configuration;
using DevPortfolio.Application.Interfaces;
using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Enums;
using DevPortfolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DevPortfolio.API.Test.Infraestrutura;

// Sobe a API em memória trocando o MySQL por SQLite em memória.
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string SenhaPadrao = "Senha@123";
    public const string EmailAtivo = "diogo@email.com";
    public const string EmailInativo = "inativo@email.com";
    public const string EmailParaInativar = "temporario@email.com";

    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    public ApiFactory()
    {
        _conexao.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            var descritor = services.Single(s => s.ServiceType == typeof(DbContextOptions<DevPortfolioDbContext>));
            services.Remove(descritor);
            services.AddDbContext<DevPortfolioDbContext>(options => options.UseSqlite(_conexao));

            // Limite alto para os testes não esbarrarem na proteção contra força bruta.
            services.Configure<SegurancaOptions>(options => options.LimiteTentativasLogin = 1000);

            // Nenhum e-mail real é enviado nos testes.
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);
        });
    }

    public EmailServiceCapturado Emails { get; } = new();

    public HttpClient CriarCliente() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            // Os cookies são Secure: o cliente precisa usar HTTPS para reenviá-los.
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

    public async Task PrepararBancoAsync()
    {
        using var scope = Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<DevPortfolioDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (!await contexto.Database.EnsureCreatedAsync())
            return;

        contexto.Usuarios.AddRange(
            CriarUsuario(EmailAtivo, ativo: true),
            CriarUsuario(EmailInativo, ativo: false),
            CriarUsuario(EmailParaInativar, ativo: true));
        await contexto.SaveChangesAsync();

        Usuario CriarUsuario(string email, bool ativo)
        {
            var usuario = new Usuario("Diogo", email, PerfilUsuario.Administrador);
            usuario.DefinirSenhaHash(hasher.GerarHash(SenhaPadrao));
            if (!ativo)
                usuario.Inativar();
            return usuario;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _conexao.Dispose();
    }
}
