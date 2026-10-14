using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace DevPortfolio.Comunicacoes.Email;

// Consome a FilaEmail e envia pelo Resend. Erros são registrados em log e não interrompem a fila.
public class EnvioEmailWorker : BackgroundService
{
    private readonly FilaEmail _fila;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<EmailOptions> _opcoes;
    private readonly ILogger<EnvioEmailWorker> _logger;

    public EnvioEmailWorker(FilaEmail fila, IServiceScopeFactory scopeFactory, IOptionsMonitor<EmailOptions> opcoes,
        ILogger<EnvioEmailWorker> logger)
    {
        _fila = fila;
        _scopeFactory = scopeFactory;
        _opcoes = opcoes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var mensagem in _fila.LerTodasAsync(stoppingToken))
        {
            await EnviarAsync(mensagem, stoppingToken);
        }
    }

    private async Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken)
    {
        var opcoes = _opcoes.CurrentValue;
        if (!opcoes.Configurado)
        {
            _logger.LogWarning("E-mail '{Tipo}' não enviado: Email:ResendApiKey não configurada.", mensagem.Tipo);
            return;
        }

        try
        {
            // O cliente do Resend depende de IOptionsSnapshot (scoped): cria um escopo por envio.
            using var scope = _scopeFactory.CreateScope();
            var resend = scope.ServiceProvider.GetRequiredService<IResend>();

            await resend.EmailSendAsync(new EmailMessage
            {
                From = opcoes.Remetente,
                To = mensagem.Para,
                Subject = mensagem.Assunto,
                HtmlBody = mensagem.Html,
            }, cancellationToken);

            _logger.LogInformation("E-mail '{Tipo}' enviado.", mensagem.Tipo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falha ao enviar o e-mail '{Tipo}'.", mensagem.Tipo);
        }
    }
}
