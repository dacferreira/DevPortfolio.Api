using System.Net;
using DevPortfolio.API.Configuration;
using DevPortfolio.API.Test.Infraestrutura;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DevPortfolio.API.Test.Auth;

// Simula a API atrás do Vercel + balanceador: a requisição chega em HTTP, com X-Forwarded-*.
public class ProxyReversoTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ProxyReversoTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CriarClienteHttp(int quantidadeProxies) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.Configure<SegurancaOptions>(options => options.QuantidadeProxies = quantidadeProxies)))
            .CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });

    private static HttpRequestMessage RequisicaoViaProxy() =>
        new(HttpMethod.Get, "/api/auth/csrf")
        {
            Headers =
            {
                { "X-Forwarded-Proto", "https" },
                { "X-Forwarded-For", "203.0.113.10, 76.76.21.21" },
            },
        };

    [Fact]
    public async Task Deve_aceitar_https_informado_pelo_proxy()
    {
        var resposta = await CriarClienteHttp(2).SendAsync(RequisicaoViaProxy());

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Contains(ClienteApi.Cookies(resposta), c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal)
            && c.Contains("secure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Sem_proxies_configurados_deve_ignorar_os_headers()
    {
        var resposta = await CriarClienteHttp(0).SendAsync(RequisicaoViaProxy());

        // Requisição HTTP com cookies exigidos como Secure: o antiforgery recusa.
        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
    }
}
