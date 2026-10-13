using DevPortfolio.Domain.Enums;

namespace DevPortfolio.Domain.Entities;

public class Usuario
{
    // Construtor usado pelo EF Core.
    private Usuario()
    {
        Nome = string.Empty;
        Email = string.Empty;
        SenhaHash = string.Empty;
    }

    public Usuario(string nome, string email, PerfilUsuario perfil)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome é obrigatório.", nameof(nome));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("O e-mail é obrigatório.", nameof(email));

        Nome = nome.Trim();
        Email = NormalizarEmail(email);
        Perfil = perfil;
        SenhaHash = string.Empty;
        Ativo = true;
        DataCadastro = DateTime.UtcNow;
    }

    public long Id { get; private set; }
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string SenhaHash { get; private set; }
    public PerfilUsuario Perfil { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime DataCadastro { get; private set; }
    public DateTime? DataUltimoAcesso { get; private set; }

    // Recebe sempre o hash: a entidade nunca conhece a senha em texto puro.
    public void DefinirSenhaHash(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("O hash da senha é obrigatório.", nameof(senhaHash));

        SenhaHash = senhaHash;
    }

    public void RegistrarAcesso() => DataUltimoAcesso = DateTime.UtcNow;

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
