using DevPortfolio.Application.Auth;
using DevPortfolio.Application.Auth.Dtos;
using DevPortfolio.Application.Test.Fakes;
using DevPortfolio.Domain.Entities;
using DevPortfolio.Domain.Enums;

namespace DevPortfolio.Application.Test.Auth;

public class AuthServiceTests
{
    private const string SenhaCorreta = "Senha@123";

    private readonly UsuarioRepositoryFake _repository = new();
    private readonly PasswordHasherFake _hasher = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_repository, _hasher);
    }

    private async Task<Usuario> CadastrarAsync(string email = "diogo@email.com", bool ativo = true)
    {
        var usuario = new Usuario("Diogo", email, PerfilUsuario.Administrador);
        usuario.DefinirSenhaHash(_hasher.GerarHash(SenhaCorreta));
        if (!ativo)
            usuario.Inativar();

        await _repository.AdicionarAsync(usuario);
        return usuario;
    }

    [Fact]
    public async Task Deve_autenticar_com_credenciais_validas()
    {
        var usuario = await CadastrarAsync();

        var resultado = await _service.AutenticarAsync(new LoginRequest { Email = "diogo@email.com", Senha = SenhaCorreta });

        Assert.True(resultado.Sucesso);
        Assert.Equal(usuario.Id, resultado.Usuario!.Id);
        Assert.Equal("Administrador", resultado.Usuario.Perfil);
        Assert.NotNull(usuario.DataUltimoAcesso);
        Assert.Equal(1, _repository.TotalSalvamentos);
    }

    [Fact]
    public async Task Deve_ignorar_maiusculas_e_espacos_no_email()
    {
        await CadastrarAsync();

        var resultado = await _service.AutenticarAsync(new LoginRequest { Email = "  DIOGO@Email.com ", Senha = SenhaCorreta });

        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public async Task Deve_recusar_usuario_inexistente_simulando_a_verificacao_da_senha()
    {
        var resultado = await _service.AutenticarAsync(new LoginRequest { Email = "naoexiste@email.com", Senha = SenhaCorreta });

        Assert.False(resultado.Sucesso);
        Assert.Equal(FalhaAutenticacao.CredenciaisInvalidas, resultado.Falha);
        Assert.Equal(1, _hasher.TotalSimulacoes);
    }

    [Fact]
    public async Task Deve_recusar_senha_incorreta()
    {
        var usuario = await CadastrarAsync();

        var resultado = await _service.AutenticarAsync(new LoginRequest { Email = "diogo@email.com", Senha = "errada" });

        Assert.False(resultado.Sucesso);
        Assert.Equal(FalhaAutenticacao.CredenciaisInvalidas, resultado.Falha);
        Assert.Null(usuario.DataUltimoAcesso);
        Assert.Equal(0, _repository.TotalSalvamentos);
    }

    [Fact]
    public async Task Deve_recusar_usuario_inativo_com_senha_correta()
    {
        await CadastrarAsync(ativo: false);

        var resultado = await _service.AutenticarAsync(new LoginRequest { Email = "diogo@email.com", Senha = SenhaCorreta });

        Assert.False(resultado.Sucesso);
        Assert.Equal(FalhaAutenticacao.UsuarioInativo, resultado.Falha);
    }

    [Fact]
    public async Task Nao_deve_revelar_que_o_usuario_esta_inativo_quando_a_senha_esta_errada()
    {
        await CadastrarAsync(ativo: false);

        var resultado = await _service.AutenticarAsync(new LoginRequest { Email = "diogo@email.com", Senha = "errada" });

        Assert.Equal(FalhaAutenticacao.CredenciaisInvalidas, resultado.Falha);
    }

    [Fact]
    public async Task Deve_obter_apenas_usuario_ativo()
    {
        var ativo = await CadastrarAsync("ativo@email.com");
        var inativo = await CadastrarAsync("inativo@email.com", ativo: false);

        Assert.NotNull(await _service.ObterUsuarioAtivoAsync(ativo.Id));
        Assert.Null(await _service.ObterUsuarioAtivoAsync(inativo.Id));
        Assert.Null(await _service.ObterUsuarioAtivoAsync(999));
    }
}
