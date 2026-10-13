namespace DevPortfolio.API.Configuration;

public class SegurancaOptions
{
    public const string Secao = "Seguranca";

    /// <summary>
    /// Marca os cookies como Secure sempre. Desligado apenas no ambiente de desenvolvimento,
    /// em que a API roda em HTTP (localhost:5128); aí o atributo segue o protocolo da requisição.
    /// </summary>
    public bool ExigirHttps { get; set; } = true;

    /// <summary>Duração da sessão, renovada a cada uso (expiração deslizante).</summary>
    public int DuracaoSessaoMinutos { get; set; } = 60;

    /// <summary>Tentativas de login permitidas por IP dentro da janela.</summary>
    public int LimiteTentativasLogin { get; set; } = 5;

    public int JanelaTentativasLoginSegundos { get; set; } = 60;
}

public class UsuarioInicialOptions
{
    public const string Secao = "UsuarioInicial";

    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? Senha { get; set; }

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(Nome) && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Senha);
}
