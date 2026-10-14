using DevPortfolio.Application.Conta;
using DevPortfolio.Application.Conta.Dtos;

namespace DevPortfolio.Application.Interfaces;

public interface IContaService
{
    Task<ResultadoRegistro> RegistrarAsync(RegistroRequest request, CancellationToken cancellationToken = default);

    // Não informa se o e-mail existe: o resultado é o mesmo para qualquer endereço.
    Task SolicitarRedefinicaoSenhaAsync(EsqueciSenhaRequest request, CancellationToken cancellationToken = default);

    Task<bool> RedefinirSenhaAsync(RedefinirSenhaRequest request, CancellationToken cancellationToken = default);
}
