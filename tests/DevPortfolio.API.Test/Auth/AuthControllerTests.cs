using System.Net;
using DevPortfolio.API.Configuration;
using DevPortfolio.API.Contracts;
using DevPortfolio.API.Test.Infraestrutura;
using DevPortfolio.Application.Auth.Dtos;
using DevPortfolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevPortfolio.API.Test.Auth;

public class AuthControllerTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly ClienteApi _cliente;

    public AuthControllerTests(ApiFactory factory)
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

    [Fact]
    public async Task Login_valido_deve_retornar_usuario_e_cookie_de_sessao_seguro()
    {
        var resposta = await _cliente.LoginAsync(ApiFactory.EmailAtivo, ApiFactory.SenhaPadrao);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await ClienteApi.LerAsync<UsuarioAutenticadoResponse>(resposta);
        Assert.True(corpo.Sucesso);
        Assert.Equal(ApiFactory.EmailAtivo, corpo.Dados!.Email);
        Assert.Null(corpo.Erro);

        var cookieSessao = ClienteApi.Cookies(resposta)
            .Single(c => c.StartsWith(SegurancaExtensions.CookieSessao + "=", StringComparison.Ordinal))
            .ToLowerInvariant();
        Assert.Contains("httponly", cookieSessao);
        Assert.Contains("secure", cookieSessao);
        Assert.Contains("samesite=strict", cookieSessao);
        Assert.DoesNotContain("expires", cookieSessao);
    }

    [Fact]
    public async Task Login_com_lembrar_de_mim_deve_gerar_cookie_persistente()
    {
        var resposta = await _cliente.LoginAsync(ApiFactory.EmailAtivo, ApiFactory.SenhaPadrao, lembrarDeMim: true);

        var cookieSessao = ClienteApi.Cookies(resposta)
            .Single(c => c.StartsWith(SegurancaExtensions.CookieSessao + "=", StringComparison.Ordinal));
        Assert.Contains("expires", cookieSessao, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_com_senha_incorreta_deve_retornar_401()
    {
        var resposta = await _cliente.LoginAsync(ApiFactory.EmailAtivo, "senha-errada");

        await AssertFalhaAsync(resposta, HttpStatusCode.Unauthorized, CodigosErro.CredenciaisInvalidas);
        Assert.DoesNotContain(ClienteApi.Cookies(resposta), c => c.StartsWith(SegurancaExtensions.CookieSessao, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_de_usuario_inexistente_deve_retornar_a_mesma_resposta_de_senha_incorreta()
    {
        var resposta = await _cliente.LoginAsync("naoexiste@email.com", ApiFactory.SenhaPadrao);

        await AssertFalhaAsync(resposta, HttpStatusCode.Unauthorized, CodigosErro.CredenciaisInvalidas);
    }

    [Fact]
    public async Task Login_de_usuario_inativo_deve_retornar_403()
    {
        var resposta = await _cliente.LoginAsync(ApiFactory.EmailInativo, ApiFactory.SenhaPadrao);

        await AssertFalhaAsync(resposta, HttpStatusCode.Forbidden, CodigosErro.UsuarioInativo);
    }

    [Fact]
    public async Task Login_com_dados_invalidos_deve_retornar_erros_por_campo()
    {
        var resposta = await _cliente.LoginAsync("email-invalido", "");

        var corpo = await AssertFalhaAsync(resposta, HttpStatusCode.BadRequest, CodigosErro.DadosInvalidos);
        Assert.Contains("email", corpo.Erro!.Detalhes!.Keys);
        Assert.Contains("senha", corpo.Erro.Detalhes.Keys);
    }

    [Fact]
    public async Task Login_sem_token_csrf_deve_ser_recusado()
    {
        var resposta = await _cliente.PostAsync("/api/auth/login",
            new { email = ApiFactory.EmailAtivo, senha = ApiFactory.SenhaPadrao }, enviarTokenCsrf: false);

        await AssertFalhaAsync(resposta, HttpStatusCode.BadRequest, CodigosErro.CsrfInvalido);
    }

    [Fact]
    public async Task Endpoint_protegido_sem_sessao_deve_retornar_401()
    {
        var resposta = await _cliente.GetAsync("/api/auth/me");

        await AssertFalhaAsync(resposta, HttpStatusCode.Unauthorized, CodigosErro.NaoAutenticado);
    }

    [Fact]
    public async Task Endpoint_protegido_com_sessao_deve_retornar_o_usuario()
    {
        await _cliente.LoginAsync(ApiFactory.EmailAtivo, ApiFactory.SenhaPadrao);

        var resposta = await _cliente.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await ClienteApi.LerAsync<UsuarioAutenticadoResponse>(resposta);
        Assert.Equal(ApiFactory.EmailAtivo, corpo.Dados!.Email);
    }

    [Fact]
    public async Task Logout_deve_encerrar_a_sessao()
    {
        await _cliente.LoginAsync(ApiFactory.EmailAtivo, ApiFactory.SenhaPadrao);

        var logout = await _cliente.PostAsync("/api/auth/logout");
        var me = await _cliente.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Logout_sem_sessao_deve_retornar_401()
    {
        var resposta = await _cliente.PostAsync("/api/auth/logout");

        await AssertFalhaAsync(resposta, HttpStatusCode.Unauthorized, CodigosErro.NaoAutenticado);
    }

    [Fact]
    public async Task Logout_sem_token_csrf_deve_ser_recusado()
    {
        await _cliente.LoginAsync(ApiFactory.EmailAtivo, ApiFactory.SenhaPadrao);

        var resposta = await _cliente.PostAsync("/api/auth/logout", enviarTokenCsrf: false);

        await AssertFalhaAsync(resposta, HttpStatusCode.BadRequest, CodigosErro.CsrfInvalido);
    }

    [Fact]
    public async Task Usuario_inativado_depois_do_login_deve_perder_a_sessao()
    {
        await _cliente.LoginAsync(ApiFactory.EmailParaInativar, ApiFactory.SenhaPadrao);

        using (var scope = _factory.Services.CreateScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<DevPortfolioDbContext>();
            var usuario = await contexto.Usuarios.SingleAsync(u => u.Email == ApiFactory.EmailParaInativar);
            usuario.Inativar();
            await contexto.SaveChangesAsync();
        }

        var resposta = await _cliente.GetAsync("/api/auth/me");

        await AssertFalhaAsync(resposta, HttpStatusCode.Unauthorized, CodigosErro.NaoAutenticado);
    }

    [Fact]
    public async Task Deve_bloquear_excesso_de_tentativas_de_login()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<SegurancaOptions>(options => options.LimiteTentativasLogin = 2)));
        var cliente = new ClienteApi(factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") }));
        await cliente.ObterTokenCsrfAsync();

        await cliente.LoginAsync(ApiFactory.EmailAtivo, "errada");
        await cliente.LoginAsync(ApiFactory.EmailAtivo, "errada");
        var resposta = await cliente.LoginAsync(ApiFactory.EmailAtivo, ApiFactory.SenhaPadrao);

        await AssertFalhaAsync(resposta, (HttpStatusCode)429, CodigosErro.MuitasTentativas);
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
