using DevPortfolio.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace DevPortfolio.Comunicacoes.Email;

public class EmailService : IEmailService
{
    private readonly FilaEmail _fila;
    private readonly EmailOptions _opcoes;
    private readonly TimeProvider _relogio;

    public EmailService(FilaEmail fila, IOptions<EmailOptions> opcoes, TimeProvider relogio)
    {
        _fila = fila;
        _opcoes = opcoes.Value;
        _relogio = relogio;
    }

    public Task EnviarBoasVindasAsync(string nome, string email, CancellationToken cancellationToken = default)
    {
        var html = TemplateEmail.Renderizar(TemplateEmail.Registro, ValoresComuns(new()
        {
            ["NOME"] = nome,
            ["EMAIL"] = email,
        }));

        return _fila.EnfileirarAsync(new MensagemEmail("boas-vindas", email, "Bem-vindo ao Diogo.dev", html),
            cancellationToken).AsTask();
    }

    public Task EnviarRedefinicaoSenhaAsync(string nome, string email, string token, int expiracaoMinutos,
        CancellationToken cancellationToken = default)
    {
        var link = $"{UrlAplicacao}/redefinir-senha?token={Uri.EscapeDataString(token)}";
        var html = TemplateEmail.Renderizar(TemplateEmail.EsqueciSenha, ValoresComuns(new()
        {
            ["NOME"] = nome,
            ["RESET_PASSWORD_URL"] = link,
            ["EXPIRACAO_MINUTOS"] = expiracaoMinutos.ToString(),
        }));

        return _fila.EnfileirarAsync(new MensagemEmail("redefinicao-senha", email, "Redefinição de senha — Diogo.dev", html),
            cancellationToken).AsTask();
    }

    private string UrlAplicacao => _opcoes.UrlAplicacao.TrimEnd('/');

    private Dictionary<string, string> ValoresComuns(Dictionary<string, string> valores)
    {
        valores["APP_URL"] = UrlAplicacao;
        valores["YEAR"] = _relogio.GetUtcNow().Year.ToString();
        return valores;
    }
}
