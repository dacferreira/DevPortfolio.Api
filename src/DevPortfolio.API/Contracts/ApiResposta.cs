namespace DevPortfolio.API.Contracts;

/// <summary>Envelope padrão de todas as respostas da API.</summary>
public class ApiResposta<T>
{
    /// <summary>Indica se a operação foi concluída com sucesso.</summary>
    public bool Sucesso { get; init; }

    /// <summary>Dados retornados quando a operação tem sucesso.</summary>
    public T? Dados { get; init; }

    /// <summary>Detalhes do erro quando a operação falha.</summary>
    public ApiErro? Erro { get; init; }
}

/// <summary>Erro padronizado.</summary>
/// <param name="Codigo">Código estável, para tratamento no cliente (ex.: CREDENCIAIS_INVALIDAS).</param>
/// <param name="Mensagem">Mensagem para exibição ao usuário.</param>
/// <param name="Detalhes">Erros por campo, quando houver.</param>
public record ApiErro(string Codigo, string Mensagem, IDictionary<string, string[]>? Detalhes = null);

public static class ApiResposta
{
    public static ApiResposta<T> Ok<T>(T dados) => new() { Sucesso = true, Dados = dados };

    public static ApiResposta<object> Falha(string codigo, string mensagem, IDictionary<string, string[]>? detalhes = null) =>
        new() { Sucesso = false, Erro = new ApiErro(codigo, mensagem, detalhes) };
}

public static class CodigosErro
{
    public const string CredenciaisInvalidas = "CREDENCIAIS_INVALIDAS";
    public const string UsuarioInativo = "USUARIO_INATIVO";
    public const string NaoAutenticado = "NAO_AUTENTICADO";
    public const string AcessoNegado = "ACESSO_NEGADO";
    public const string DadosInvalidos = "DADOS_INVALIDOS";
    public const string CsrfInvalido = "CSRF_INVALIDO";
    public const string MuitasTentativas = "MUITAS_TENTATIVAS";
    public const string ErroInterno = "ERRO_INTERNO";
}
