using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Interfaces;

namespace DevPortfolio.Application.Test.Fakes;

public class UsuarioRepositoryFake : IUsuarioRepository
{
    private readonly List<Usuario> _usuarios = new();
    private long _proximoId = 1;

    public int TotalSalvamentos { get; private set; }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(u => u.Email == Usuario.NormalizarEmail(email)));

    public Task<Usuario?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == id));

    public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        // Simula o AUTO_INCREMENT do banco.
        typeof(Usuario).GetProperty(nameof(Usuario.Id))!.SetValue(usuario, _proximoId++);
        _usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        TotalSalvamentos++;
        return Task.CompletedTask;
    }
}
