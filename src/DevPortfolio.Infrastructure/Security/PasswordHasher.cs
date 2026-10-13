using DevPortfolio.Application.Interfaces;
using DevPortfolio.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace DevPortfolio.Infrastructure.Security;

// Usa o hasher do ASP.NET Core Identity: PBKDF2 com HMAC-SHA512, salt aleatório
// por senha e 100.000 iterações (formato V3, com os parâmetros gravados no próprio hash).
public class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    // Hash de uma senha descartável, usado apenas para igualar o tempo de resposta.
    private static readonly Lazy<string> HashFicticio = new(() => Hasher.HashPassword(null!, Guid.NewGuid().ToString()));

    public string GerarHash(string senha)
    {
        if (string.IsNullOrEmpty(senha))
            throw new ArgumentException("A senha é obrigatória.", nameof(senha));

        return Hasher.HashPassword(null!, senha);
    }

    public bool Verificar(string senhaHash, string senha)
    {
        if (string.IsNullOrEmpty(senhaHash) || string.IsNullOrEmpty(senha))
            return false;

        try
        {
            var resultado = Hasher.VerifyHashedPassword(null!, senhaHash, senha);
            return resultado is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // Hash gravado em formato inválido: trata como senha incorreta.
            return false;
        }
    }

    public void SimularVerificacao(string senha) => Verificar(HashFicticio.Value, senha ?? string.Empty);
}
