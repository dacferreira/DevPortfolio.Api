namespace DevPortfolio.Application.Interfaces;

// Envio de e-mails transacionais (implementado em DevPortfolio.Comunicacoes).
// Os métodos apenas enfileiram: uma falha de envio não interrompe o fluxo de quem chamou.
public interface IEmailService
{
    Task EnviarBoasVindasAsync(string nome, string email, CancellationToken cancellationToken = default);

    Task EnviarRedefinicaoSenhaAsync(string nome, string email, string token, int expiracaoMinutos,
        CancellationToken cancellationToken = default);
}
