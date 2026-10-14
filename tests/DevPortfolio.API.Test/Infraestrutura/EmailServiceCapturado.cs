using System.Collections.Concurrent;
using DevPortfolio.Application.Interfaces;

namespace DevPortfolio.API.Test.Infraestrutura;

// Substitui o envio real (Resend) nos testes de integração, guardando o que seria enviado.
public class EmailServiceCapturado : IEmailService
{
    public ConcurrentQueue<(string Nome, string Email)> BoasVindas { get; } = new();
    public ConcurrentQueue<(string Email, string Token)> Redefinicoes { get; } = new();

    public Task EnviarBoasVindasAsync(string nome, string email, CancellationToken cancellationToken = default)
    {
        BoasVindas.Enqueue((nome, email));
        return Task.CompletedTask;
    }

    public Task EnviarRedefinicaoSenhaAsync(string nome, string email, string token, int expiracaoMinutos,
        CancellationToken cancellationToken = default)
    {
        Redefinicoes.Enqueue((email, token));
        return Task.CompletedTask;
    }
}
