using System.Security.Cryptography;
using System.Text;

namespace DevPortfolio.Application.Conta;

public static class TokenSeguro
{
    // 32 bytes aleatórios em Base64 URL-safe (vai no link do e-mail).
    public static string Gerar() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    // SHA-256 em hexadecimal: é o que fica gravado no banco.
    public static string CalcularHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
