namespace DevPortfolio.Application.Interfaces;

public interface IPasswordHasher
{
    string GerarHash(string senha);

    bool Verificar(string senhaHash, string senha);

    // Executa uma verificação com custo equivalente, para que "usuário inexistente"
    // e "senha incorreta" levem o mesmo tempo e não revelem quais e-mails existem.
    void SimularVerificacao(string senha);
}
