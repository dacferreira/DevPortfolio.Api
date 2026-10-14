using System.ComponentModel.DataAnnotations;

namespace DevPortfolio.Application.Conta.Dtos;

/// <summary>Solicitação de link para redefinir a senha.</summary>
public class EsqueciSenhaRequest
{
    /// <summary>E-mail da conta.</summary>
    /// <example>maria@email.com</example>
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [MaxLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;
}
