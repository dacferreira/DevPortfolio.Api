using System.Net.Http.Json;
using System.Text.Json;
using DevPortfolio.API.Contracts;

namespace DevPortfolio.API.Test.Infraestrutura;

// Simula o navegador: guarda os cookies e envia o token anti-CSRF como o Angular faz.
public class ClienteApi
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private string? _tokenCsrf;

    public ClienteApi(HttpClient http)
    {
        _http = http;
    }

    public async Task ObterTokenCsrfAsync()
    {
        var resposta = await _http.GetAsync("/api/auth/csrf");
        resposta.EnsureSuccessStatusCode();
        AtualizarTokenCsrf(resposta);
    }

    public Task<HttpResponseMessage> LoginAsync(string email, string senha, bool lembrarDeMim = false) =>
        PostAsync("/api/auth/login", new { email, senha, lembrarDeMim });

    public Task<HttpResponseMessage> GetAsync(string url) => _http.GetAsync(url);

    public async Task<HttpResponseMessage> PostAsync(string url, object? corpo = null, bool enviarTokenCsrf = true)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(corpo ?? new { }),
        };

        if (enviarTokenCsrf && _tokenCsrf is not null)
            requisicao.Headers.Add("X-XSRF-TOKEN", _tokenCsrf);

        var resposta = await _http.SendAsync(requisicao);
        AtualizarTokenCsrf(resposta);
        return resposta;
    }

    public static async Task<ApiResposta<T>> LerAsync<T>(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<ApiResposta<T>>(OpcoesJson))!;

    public static IEnumerable<string> Cookies(HttpResponseMessage resposta) =>
        resposta.Headers.TryGetValues("Set-Cookie", out var valores) ? valores : Enumerable.Empty<string>();

    private void AtualizarTokenCsrf(HttpResponseMessage resposta)
    {
        var cookie = Cookies(resposta).FirstOrDefault(c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        if (cookie is not null)
            _tokenCsrf = Uri.UnescapeDataString(cookie.Split(';')[0]["XSRF-TOKEN=".Length..]);
    }
}
