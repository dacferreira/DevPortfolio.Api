using DevPortfolio.Application.Auth.Dtos;

namespace DevPortfolio.Application.Conta;

public class ResultadoRegistro
{
    private ResultadoRegistro(UsuarioAutenticadoResponse? usuario)
    {
        Usuario = usuario;
    }

    public UsuarioAutenticadoResponse? Usuario { get; }
    public bool Sucesso => Usuario is not null;

    public static ResultadoRegistro Ok(UsuarioAutenticadoResponse usuario) => new(usuario);

    public static ResultadoRegistro EmailJaCadastrado() => new(null);
}
