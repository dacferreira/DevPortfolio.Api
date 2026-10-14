using System.ComponentModel.DataAnnotations;

namespace DevPortfolio.Application.Conta.Dtos;

/// <summary>Nova senha, com o token recebido por e-mail.</summary>
public class RedefinirSenhaRequest
{
    /// <summary>Token do link enviado por e-mail.</summary>
    [Required(ErrorMessage = "Link de redefinição inválido.")]
    [MaxLength(200, ErrorMessage = "Link de redefinição inválido.")]
    public string Token { get; set; } = string.Empty;

    /// <summary>Nova senha: de 8 a 128 caracteres, com letras e números.</summary>
    [Required(ErrorMessage = "Informe a nova senha.")]
    [RegularExpression(RegrasSenha.Padrao, ErrorMessage = RegrasSenha.Mensagem)]
    public string NovaSenha { get; set; } = string.Empty;

    /// <summary>Repetição da nova senha.</summary>
    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}
