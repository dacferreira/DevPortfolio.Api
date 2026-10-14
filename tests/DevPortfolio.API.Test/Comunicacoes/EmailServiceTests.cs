using DevPortfolio.Comunicacoes.Email;
using Microsoft.Extensions.Options;

namespace DevPortfolio.API.Test.Comunicacoes;

public class EmailServiceTests
{
    private readonly FilaEmail _fila = new();
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        var opcoes = Options.Create(new EmailOptions { UrlAplicacao = "https://diogo.dev/" });
        _service = new EmailService(_fila, opcoes, TimeProvider.System);
    }

    private async Task<MensagemEmail> ProximaMensagemAsync()
    {
        using var cancelamento = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var mensagem in _fila.LerTodasAsync(cancelamento.Token))
            return mensagem;
        throw new InvalidOperationException("Nenhuma mensagem enfileirada.");
    }

    [Fact]
    public async Task Boas_vindas_deve_preencher_o_template_de_registro()
    {
        await _service.EnviarBoasVindasAsync("Maria", "maria@email.com");

        var mensagem = await ProximaMensagemAsync();
        Assert.Equal("maria@email.com", mensagem.Para);
        Assert.Equal("Bem-vindo ao Diogo.dev", mensagem.Assunto);
        Assert.Contains("Bem-vindo, Maria!", mensagem.Html);
        Assert.Contains("maria@email.com", mensagem.Html);
        Assert.Contains("href=\"https://diogo.dev/login\"", mensagem.Html);
        Assert.Contains(DateTime.UtcNow.Year.ToString(), mensagem.Html);
        Assert.DoesNotContain("{{", mensagem.Html);
    }

    [Fact]
    public async Task Redefinicao_deve_conter_o_link_com_o_token_e_a_validade()
    {
        await _service.EnviarRedefinicaoSenhaAsync("Maria", "maria@email.com", "abc-123_XYZ", 30);

        var mensagem = await ProximaMensagemAsync();
        Assert.Contains("https://diogo.dev/redefinir-senha?token=abc-123_XYZ", mensagem.Html);
        Assert.Contains("30 minutos", mensagem.Html);
        Assert.DoesNotContain("{{", mensagem.Html);
    }

    [Fact]
    public async Task Valores_devem_ser_codificados_em_html()
    {
        await _service.EnviarBoasVindasAsync("<script>alert(1)</script>", "maria@email.com");

        var mensagem = await ProximaMensagemAsync();
        Assert.DoesNotContain("<script>", mensagem.Html);
        Assert.Contains("&lt;script&gt;", mensagem.Html);
    }
}
