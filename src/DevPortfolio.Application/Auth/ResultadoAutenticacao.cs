using DevPortfolio.Application.Auth.Dtos;

namespace DevPortfolio.Application.Auth;

public enum FalhaAutenticacao
{
    CredenciaisInvalidas,
    UsuarioInativo,
}

public class ResultadoAutenticacao
{
    private ResultadoAutenticacao(UsuarioAutenticadoResponse? usuario, FalhaAutenticacao? falha)
    {
        Usuario = usuario;
        Falha = falha;
    }

    public UsuarioAutenticadoResponse? Usuario { get; }
    public FalhaAutenticacao? Falha { get; }
    public bool Sucesso => Falha is null;

    public static ResultadoAutenticacao Ok(UsuarioAutenticadoResponse usuario) => new(usuario, null);

    public static ResultadoAutenticacao Falhou(FalhaAutenticacao falha) => new(null, falha);
}
