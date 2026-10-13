using DevPortfolio.Application.Interfaces;

namespace DevPortfolio.Application.Test.Fakes;

public class PasswordHasherFake : IPasswordHasher
{
    public int TotalSimulacoes { get; private set; }

    public string GerarHash(string senha) => $"hash:{senha}";

    public bool Verificar(string senhaHash, string senha) => senhaHash == GerarHash(senha);

    public void SimularVerificacao(string senha) => TotalSimulacoes++;
}
