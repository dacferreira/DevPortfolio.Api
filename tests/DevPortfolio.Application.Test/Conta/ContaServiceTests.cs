using DevPortfolio.Application.Conta;
using DevPortfolio.Application.Conta.Dtos;
using DevPortfolio.Application.Test.Fakes;
using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Enums;
using Microsoft.Extensions.Options;

namespace DevPortfolio.Application.Test.Conta;

public class ContaServiceTests
{
    private readonly UsuarioRepositoryFake _usuarios = new();
    private readonly TokenRedefinicaoSenhaRepositoryFake _tokens = new();
    private readonly PasswordHasherFake _hasher = new();
    private readonly EmailServiceFake _emails = new();
    private readonly RelogioFake _relogio = new();
    private readonly ContaService _service;

    public ContaServiceTests()
    {
        _service = new ContaService(_usuarios, _tokens, _hasher, _emails, _relogio,
            Options.Create(new ContaOptions { ExpiracaoTokenRedefinicaoMinutos = 30 }));
    }

    private static RegistroRequest Registro(string email = "maria@email.com") => new()
    {
        Nome = "Maria",
        Email = email,
        Senha = "Senha123",
        ConfirmarSenha = "Senha123",
        AceitouTermos = true,
    };

    private async Task<Usuario> CadastrarAsync(string email = "maria@email.com", bool ativo = true)
    {
        var usuario = new Usuario("Maria", email, PerfilUsuario.Visitante);
        usuario.DefinirSenhaHash(_hasher.GerarHash("Senha123"));
        if (!ativo)
            usuario.Inativar();
        await _usuarios.AdicionarAsync(usuario);
        return usuario;
    }

    private async Task<string> SolicitarTokenAsync(string email = "maria@email.com")
    {
        await _service.SolicitarRedefinicaoSenhaAsync(new EsqueciSenhaRequest { Email = email });
        return _emails.Redefinicoes.Last().Token;
    }

    private static RedefinirSenhaRequest Redefinicao(string token, string senha = "NovaSenha1") =>
        new() { Token = token, NovaSenha = senha, ConfirmarSenha = senha };

    [Fact]
    public async Task Registro_deve_criar_visitante_com_senha_em_hash_e_enviar_boas_vindas()
    {
        var resultado = await _service.RegistrarAsync(Registro());

        Assert.True(resultado.Sucesso);
        Assert.Equal("Visitante", resultado.Usuario!.Perfil);

        var usuario = await _usuarios.ObterPorEmailAsync("maria@email.com");
        Assert.Equal(PerfilUsuario.Visitante, usuario!.Perfil);
        Assert.Equal("hash:Senha123", usuario.SenhaHash);
        Assert.Equal(("Maria", "maria@email.com"), Assert.Single(_emails.BoasVindas));
    }

    [Fact]
    public async Task Registro_nunca_deve_criar_administrador()
    {
        await _service.RegistrarAsync(Registro());

        var usuario = await _usuarios.ObterPorEmailAsync("maria@email.com");
        Assert.NotEqual(PerfilUsuario.Administrador, usuario!.Perfil);
    }

    [Fact]
    public async Task Registro_deve_recusar_email_ja_cadastrado_sem_enviar_email()
    {
        await CadastrarAsync();

        var resultado = await _service.RegistrarAsync(Registro(" MARIA@email.com "));

        Assert.False(resultado.Sucesso);
        Assert.Empty(_emails.BoasVindas);
    }

    [Fact]
    public async Task Esqueci_senha_deve_gravar_somente_o_hash_do_token_e_enviar_o_link()
    {
        var usuario = await CadastrarAsync();

        var token = await SolicitarTokenAsync();

        var gravado = Assert.Single(_tokens.Tokens);
        Assert.Equal(usuario.Id, gravado.UsuarioId);
        Assert.NotEqual(token, gravado.TokenHash);
        Assert.Equal(TokenSeguro.CalcularHash(token), gravado.TokenHash);
        Assert.Equal(_relogio.Agora.UtcDateTime.AddMinutes(30), gravado.DataExpiracao);
        Assert.Equal(30, _emails.Redefinicoes.Single().ExpiracaoMinutos);
    }

    [Fact]
    public async Task Esqueci_senha_nao_deve_fazer_nada_para_email_inexistente()
    {
        await _service.SolicitarRedefinicaoSenhaAsync(new EsqueciSenhaRequest { Email = "naoexiste@email.com" });

        Assert.Empty(_tokens.Tokens);
        Assert.Empty(_emails.Redefinicoes);
    }

    [Fact]
    public async Task Esqueci_senha_nao_deve_fazer_nada_para_usuario_inativo()
    {
        await CadastrarAsync(ativo: false);

        await _service.SolicitarRedefinicaoSenhaAsync(new EsqueciSenhaRequest { Email = "maria@email.com" });

        Assert.Empty(_emails.Redefinicoes);
    }

    [Fact]
    public async Task Novo_pedido_deve_invalidar_o_link_anterior()
    {
        await CadastrarAsync();
        var primeiro = await SolicitarTokenAsync();
        await SolicitarTokenAsync();

        Assert.False(await _service.RedefinirSenhaAsync(Redefinicao(primeiro)));
    }

    [Fact]
    public async Task Redefinir_deve_trocar_a_senha_e_invalidar_o_token()
    {
        var usuario = await CadastrarAsync();
        var token = await SolicitarTokenAsync();

        Assert.True(await _service.RedefinirSenhaAsync(Redefinicao(token)));

        Assert.Equal("hash:NovaSenha1", usuario.SenhaHash);
        Assert.False(await _service.RedefinirSenhaAsync(Redefinicao(token, "OutraSenha2")));
        Assert.Equal("hash:NovaSenha1", usuario.SenhaHash);
    }

    [Fact]
    public async Task Redefinir_deve_recusar_token_expirado()
    {
        await CadastrarAsync();
        var token = await SolicitarTokenAsync();
        _relogio.Avancar(TimeSpan.FromMinutes(31));

        Assert.False(await _service.RedefinirSenhaAsync(Redefinicao(token)));
    }

    [Fact]
    public async Task Redefinir_deve_recusar_token_inexistente()
    {
        Assert.False(await _service.RedefinirSenhaAsync(Redefinicao("token-qualquer")));
    }
}
