namespace DevPortfolio.Comunicacoes.Email;

public class EmailOptions
{
    public const string Secao = "Email";

    /// <summary>Chave da API do Resend (segredo: user-secrets ou variável Email__ResendApiKey).</summary>
    public string? ResendApiKey { get; set; }

    /// <summary>Remetente. "onboarding@resend.dev" só entrega para o dono da conta do Resend.</summary>
    public string Remetente { get; set; } = "Diogo.dev <onboarding@resend.dev>";

    /// <summary>Endereço do front, usado nos links dos e-mails.</summary>
    public string UrlAplicacao { get; set; } = "http://localhost:4200";

    public bool Configurado => !string.IsNullOrWhiteSpace(ResendApiKey);
}
