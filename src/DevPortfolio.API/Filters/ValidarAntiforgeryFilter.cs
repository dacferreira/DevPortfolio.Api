using DevPortfolio.API.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevPortfolio.API.Filters;

// Exige o token anti-CSRF (header X-XSRF-TOKEN) em toda requisição que altera estado.
// Equivale ao AutoValidateAntiforgeryToken do MVC, sem depender dos serviços de views.
public class ValidarAntiforgeryFilter : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> MetodosSeguros = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get, HttpMethods.Head, HttpMethods.Options, HttpMethods.Trace,
    };

    private readonly IAntiforgery _antiforgery;

    public ValidarAntiforgeryFilter(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.Result is not null || MetodosSeguros.Contains(context.HttpContext.Request.Method))
            return;

        if (!await _antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            context.Result = new BadRequestObjectResult(ApiResposta.Falha(CodigosErro.CsrfInvalido,
                "Sessão expirada ou requisição inválida. Atualize a página e tente novamente."));
        }
    }
}
