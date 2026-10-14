using System.Net;
using DevPortfolio.API.Contracts;
using DevPortfolio.API.Test.Infraestrutura;
using DevPortfolio.Application.Auth.Dtos;

namespace DevPortfolio.API.Test.Conta;

public class ContaControllerTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly ClienteApi _cliente;

    public ContaControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _cliente = new ClienteApi(factory.CriarCliente());
    }

    public async Task InitializeAsync()
    {
        await _factory.PrepararBancoAsync();
        await _cliente.ObterTokenCsrfAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string EmailUnico() => $"{Guid.NewGuid():N}@email.com";

    private Task<HttpResponseMessage> RegistrarAsync(string email, string senha = "Senha123", bool aceitouTermos = true) =>
        _cliente.PostAsync("/api/conta/registro", new
        {
            nome = "Maria",
            email,
            senha,
            confirmarSenha = senha,
            aceitouTermos,
        });

    private async Task<string> SolicitarTokenAsync(string email)
    {
        var resposta = await _cliente.PostAsync("/api/conta/esqueci-senha", new { email });
        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
        return _factory.Emails.Redefinicoes.Last(r => r.Email == email).Token;
    }

    [Fact]
    public async Task Registro_deve_criar_conta_de_visitante_e_enviar_boas_vindas()
    {
        var email = EmailUnico();

        var resposta = await RegistrarAsync(email);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await ClienteApi.LerAsync<UsuarioAutenticadoResponse>(resposta);
        Assert.Equal("Visitante", corpo.Dados!.Perfil);
        Assert.Contains(_factory.Emails.BoasVindas, e => e.Email == email);
    }

    [Fact]
    public async Task Conta_criada_deve_conseguir_entrar_como_visitante()
    {
        var email = EmailUnico();
        await RegistrarAsync(email);

        var login = await _cliente.LoginAsync(email, "Senha123");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var corpo = await ClienteApi.LerAsync<UsuarioAutenticadoResponse>(login);
        Assert.Equal("Visitante", corpo.Dados!.Perfil);
    }

    [Fact]
    public async Task Registro_com_email_ja_cadastrado_deve_retornar_409()
    {
        var resposta = await RegistrarAsync(ApiFactory.EmailAtivo);

        await AssertFalhaAsync(resposta, HttpStatusCode.Conflict, CodigosErro.EmailJaCadastrado);
    }

    [Theory]
    [InlineData("curta1", "senha")]
    [InlineData("somenteletras", "senha")]
    [InlineData("12345678", "senha")]
    public async Task Registro_com_senha_fraca_deve_retornar_erro_no_campo(string senha, string campo)
    {
        var resposta = await RegistrarAsync(EmailUnico(), senha);

        var corpo = await AssertFalhaAsync(resposta, HttpStatusCode.BadRequest, CodigosErro.DadosInvalidos);
        Assert.Contains(campo, corpo.Erro!.Detalhes!.Keys);
    }

    [Fact]
    public async Task Registro_sem_aceitar_os_termos_deve_ser_recusado()
    {
        var resposta = await RegistrarAsync(EmailUnico(), aceitouTermos: false);

        var corpo = await AssertFalhaAsync(resposta, HttpStatusCode.BadRequest, CodigosErro.DadosInvalidos);
        Assert.Contains("aceitouTermos", corpo.Erro!.Detalhes!.Keys);
    }

    [Fact]
    public async Task Registro_sem_token_csrf_deve_ser_recusado()
    {
        var resposta = await _cliente.PostAsync("/api/conta/registro",
            new { nome = "Maria", email = EmailUnico(), senha = "Senha123", confirmarSenha = "Senha123", aceitouTermos = true },
            enviarTokenCsrf: false);

        await AssertFalhaAsync(resposta, HttpStatusCode.BadRequest, CodigosErro.CsrfInvalido);
    }

    [Fact]
    public async Task Esqueci_senha_deve_responder_igual_para_email_existente_e_inexistente()
    {
        var existente = await _cliente.PostAsync("/api/conta/esqueci-senha", new { email = ApiFactory.EmailAtivo });
        var inexistente = await _cliente.PostAsync("/api/conta/esqueci-senha", new { email = EmailUnico() });

        Assert.Equal(HttpStatusCode.Accepted, existente.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, inexistente.StatusCode);
        Assert.Equal(await existente.Content.ReadAsStringAsync(), await inexistente.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Redefinir_senha_deve_permitir_entrar_com_a_nova_senha_uma_unica_vez()
    {
        var email = EmailUnico();
        await RegistrarAsync(email);
        var token = await SolicitarTokenAsync(email);

        var redefinicao = await _cliente.PostAsync("/api/conta/redefinir-senha",
            new { token, novaSenha = "NovaSenha9", confirmarSenha = "NovaSenha9" });
        var reutilizacao = await _cliente.PostAsync("/api/conta/redefinir-senha",
            new { token, novaSenha = "OutraSenha9", confirmarSenha = "OutraSenha9" });

        Assert.Equal(HttpStatusCode.NoContent, redefinicao.StatusCode);
        await AssertFalhaAsync(reutilizacao, HttpStatusCode.BadRequest, CodigosErro.TokenInvalido);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente.LoginAsync(email, "Senha123")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.LoginAsync(email, "NovaSenha9")).StatusCode);
    }

    [Fact]
    public async Task Redefinir_senha_com_token_invalido_deve_retornar_400()
    {
        var resposta = await _cliente.PostAsync("/api/conta/redefinir-senha",
            new { token = "token-invalido", novaSenha = "NovaSenha9", confirmarSenha = "NovaSenha9" });

        await AssertFalhaAsync(resposta, HttpStatusCode.BadRequest, CodigosErro.TokenInvalido);
    }

    [Fact]
    public async Task Redefinir_senha_com_confirmacao_diferente_deve_retornar_erro_no_campo()
    {
        var resposta = await _cliente.PostAsync("/api/conta/redefinir-senha",
            new { token = "qualquer", novaSenha = "NovaSenha9", confirmarSenha = "Diferente9" });

        var corpo = await AssertFalhaAsync(resposta, HttpStatusCode.BadRequest, CodigosErro.DadosInvalidos);
        Assert.Contains("confirmarSenha", corpo.Erro!.Detalhes!.Keys);
    }

    private static async Task<ApiResposta<object>> AssertFalhaAsync(HttpResponseMessage resposta, HttpStatusCode status, string codigo)
    {
        Assert.Equal(status, resposta.StatusCode);
        var corpo = await ClienteApi.LerAsync<object>(resposta);
        Assert.False(corpo.Sucesso);
        Assert.Equal(codigo, corpo.Erro!.Codigo);
        return corpo;
    }
}
