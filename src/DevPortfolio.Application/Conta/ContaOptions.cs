namespace DevPortfolio.Application.Conta;

public class ContaOptions
{
    public const string Secao = "Conta";

    /// <summary>Validade do link de redefinição de senha.</summary>
    public int ExpiracaoTokenRedefinicaoMinutos { get; set; } = 30;
}
