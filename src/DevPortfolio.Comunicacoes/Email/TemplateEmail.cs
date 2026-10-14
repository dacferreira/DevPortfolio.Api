using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace DevPortfolio.Comunicacoes.Email;

// Carrega os templates embutidos (Templates/*.html) e substitui os marcadores {{CHAVE}}.
// Os valores são codificados em HTML: um nome como "<script>" não vira código no e-mail.
public static partial class TemplateEmail
{
    public const string Registro = "registro";
    public const string EsqueciSenha = "esqueci-senha";

    private static readonly ConcurrentDictionary<string, string> Cache = new();

    public static string Renderizar(string template, IReadOnlyDictionary<string, string> valores)
    {
        var html = Cache.GetOrAdd(template, Carregar);

        return Marcador().Replace(html, marcador =>
        {
            var chave = marcador.Groups[1].Value;
            return valores.TryGetValue(chave, out var valor)
                ? HtmlEncoder.Default.Encode(valor)
                : throw new InvalidOperationException($"Valor não informado para {{{{{chave}}}}} no template '{template}'.");
        });
    }

    private static string Carregar(string template)
    {
        var assembly = typeof(TemplateEmail).Assembly;
        var recurso = $"{assembly.GetName().Name}.Templates.{template}.html";

        using var stream = assembly.GetManifestResourceStream(recurso)
            ?? throw new InvalidOperationException($"Template de e-mail '{template}' não encontrado.");
        using var leitor = new StreamReader(stream);
        return leitor.ReadToEnd();
    }

    [GeneratedRegex(@"\{\{([A-Z_]+)\}\}")]
    private static partial Regex Marcador();
}
