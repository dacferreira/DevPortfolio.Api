using DevPortfolio.Domain.Entities;

namespace DevPortfolio.Application.Auth.Dtos;

/// <summary>Dados públicos do usuário autenticado.</summary>
public record UsuarioAutenticadoResponse(long Id, string Nome, string Email, string Perfil)
{
    public static UsuarioAutenticadoResponse De(Usuario usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil.ToString());
}
