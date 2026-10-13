using System.ComponentModel.DataAnnotations;

namespace DevPortfolio.Application.Auth.Dtos;

/// <summary>Credenciais para autenticação.</summary>
public class LoginRequest
{
    /// <summary>E-mail do usuário.</summary>
    /// <example>diogo@email.com</example>
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [MaxLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Senha do usuário.</summary>
    [Required(ErrorMessage = "Informe a senha.")]
    [MaxLength(128, ErrorMessage = "A senha deve ter no máximo 128 caracteres.")]
    public string Senha { get; set; } = string.Empty;

    /// <summary>Mantém a sessão após fechar o navegador (cookie persistente).</summary>
    public bool LembrarDeMim { get; set; }
}
