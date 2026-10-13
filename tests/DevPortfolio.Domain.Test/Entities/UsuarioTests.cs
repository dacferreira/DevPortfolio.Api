using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Enums;

namespace DevPortfolio.Domain.Test.Entities;

public class UsuarioTests
{
    [Fact]
    public void Deve_criar_usuario_ativo_com_email_normalizado()
    {
        var usuario = new Usuario("  Diogo ", "  Diogo@Email.COM ", PerfilUsuario.Administrador);

        Assert.Equal("Diogo", usuario.Nome);
        Assert.Equal("diogo@email.com", usuario.Email);
        Assert.True(usuario.Ativo);
        Assert.Null(usuario.DataUltimoAcesso);
    }

    [Theory]
    [InlineData("", "diogo@email.com")]
    [InlineData("Diogo", " ")]
    public void Deve_exigir_nome_e_email(string nome, string email)
    {
        Assert.Throws<ArgumentException>(() => new Usuario(nome, email, PerfilUsuario.Administrador));
    }

    [Fact]
    public void Nao_deve_aceitar_hash_de_senha_vazio()
    {
        var usuario = new Usuario("Diogo", "diogo@email.com", PerfilUsuario.Administrador);

        Assert.Throws<ArgumentException>(() => usuario.DefinirSenhaHash(""));
    }

    [Fact]
    public void Deve_inativar_e_reativar()
    {
        var usuario = new Usuario("Diogo", "diogo@email.com", PerfilUsuario.Administrador);

        usuario.Inativar();
        Assert.False(usuario.Ativo);

        usuario.Ativar();
        Assert.True(usuario.Ativo);
    }

    [Fact]
    public void Deve_registrar_ultimo_acesso()
    {
        var usuario = new Usuario("Diogo", "diogo@email.com", PerfilUsuario.Administrador);

        usuario.RegistrarAcesso();

        Assert.NotNull(usuario.DataUltimoAcesso);
    }
}
