using DevPortfolio.Application.Interfaces;

namespace DevPortfolio.Application.Test.Fakes;

public class EmailServiceFake : IEmailService
{
    public List<(string Nome, string Email)> BoasVindas { get; } = new();
    public List<(string Nome, string Email, string Token, int ExpiracaoMinutos)> Redefinicoes { get; } = new();

    public Task EnviarBoasVindasAsync(string nome, string email, CancellationToken cancellationToken = default)
    {
        BoasVindas.Add((nome, email));
        return Task.CompletedTask;
    }

    public Task EnviarRedefinicaoSenhaAsync(string nome, string email, string token, int expiracaoMinutos,
        CancellationToken cancellationToken = default)
    {
        Redefinicoes.Add((nome, email, token, expiracaoMinutos));
        return Task.CompletedTask;
    }
}
