using System.ComponentModel.DataAnnotations;

namespace DevPortfolio.Application.Conta.Dtos;

/// <summary>Dados para criar uma conta (perfil Visitante).</summary>
public class RegistroRequest
{
    /// <summary>Nome de exibição.</summary>
    /// <example>Maria Silva</example>
    [Required(ErrorMessage = "Informe o nome.")]
    [MinLength(2, ErrorMessage = "O nome deve ter ao menos 2 caracteres.")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>E-mail de login.</summary>
    /// <example>maria@email.com</example>
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [MaxLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Senha: de 8 a 128 caracteres, com letras e números.</summary>
    [Required(ErrorMessage = "Informe a senha.")]
    [RegularExpression(RegrasSenha.Padrao, ErrorMessage = RegrasSenha.Mensagem)]
    public string Senha { get; set; } = string.Empty;

    /// <summary>Repetição da senha.</summary>
    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmarSenha { get; set; } = string.Empty;

    /// <summary>Aceite dos Termos de Uso e da Política de Privacidade.</summary>
    [Range(typeof(bool), "true", "true", ErrorMessage = "É preciso aceitar os Termos de Uso e a Política de Privacidade.")]
    public bool AceitouTermos { get; set; }
}
