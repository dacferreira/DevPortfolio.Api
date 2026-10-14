namespace DevPortfolio.Domain.Entities;

// Token de uso único para redefinir a senha. Só o hash (SHA-256) é gravado:
// quem tiver acesso ao banco não consegue usar os links enviados por e-mail.
public class TokenRedefinicaoSenha
{
    // Construtor usado pelo EF Core.
    private TokenRedefinicaoSenha()
    {
        TokenHash = string.Empty;
    }

    public TokenRedefinicaoSenha(long usuarioId, string tokenHash, DateTime dataExpiracao, DateTime agora)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("O hash do token é obrigatório.", nameof(tokenHash));

        if (dataExpiracao <= agora)
            throw new ArgumentException("A expiração deve ser posterior à criação.", nameof(dataExpiracao));

        UsuarioId = usuarioId;
        TokenHash = tokenHash;
        DataExpiracao = dataExpiracao;
        DataCadastro = agora;
    }

    public long Id { get; private set; }
    public long UsuarioId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime DataExpiracao { get; private set; }
    public DateTime? DataUtilizacao { get; private set; }
    public DateTime DataCadastro { get; private set; }

    public bool EstaValido(DateTime agora) => DataUtilizacao is null && agora < DataExpiracao;

    public void MarcarComoUtilizado(DateTime agora) => DataUtilizacao ??= agora;
}
