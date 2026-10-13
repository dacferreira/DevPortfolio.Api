using System.Text.Json;
using System.Threading.RateLimiting;
using DevPortfolio.API.Contracts;
using DevPortfolio.API.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DevPortfolio.API.Configuration;

public static class SegurancaExtensions
{
    public const string PoliticaLogin = "login";
    public const string CookieSessao = "devportfolio.sessao";
    public const string CookieAntiforgery = "devportfolio.antiforgery";

    // Cookie lido pelo Angular (HttpClient) e devolvido no header X-XSRF-TOKEN.
    public const string CookieTokenCsrf = "XSRF-TOKEN";
    public const string HeaderTokenCsrf = "X-XSRF-TOKEN";

    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    public static IServiceCollection AddSeguranca(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SegurancaOptions>(configuration.GetSection(SegurancaOptions.Secao));
        var seguranca = configuration.GetSection(SegurancaOptions.Secao).Get<SegurancaOptions>() ?? new SegurancaOptions();
        var politicaSecure = seguranca.ExigirHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

        // Sessão em cookie HttpOnly (inacessível ao JavaScript), Secure e SameSite=Strict.
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = CookieSessao;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = politicaSecure;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(seguranca.DuracaoSessaoMinutos);
                options.SlidingExpiration = true;

                // API não redireciona para página de login: responde 401/403 no envelope padrão.
                options.Events.OnRedirectToLogin = contexto =>
                    EscreverFalhaAsync(contexto.HttpContext, StatusCodes.Status401Unauthorized,
                        CodigosErro.NaoAutenticado, "Faça login para continuar.");
                options.Events.OnRedirectToAccessDenied = contexto =>
                    EscreverFalhaAsync(contexto.HttpContext, StatusCodes.Status403Forbidden,
                        CodigosErro.AcessoNegado, "Você não tem permissão para acessar este recurso.");
            });

        services.AddAuthorization();

        services.AddAntiforgery(options =>
        {
            options.HeaderName = HeaderTokenCsrf;
            options.Cookie.Name = CookieAntiforgery;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = politicaSecure;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        // Limita tentativas de login por IP (proteção contra força bruta).
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PoliticaLogin, httpContext =>
            {
                var limite = httpContext.RequestServices.GetRequiredService<IOptions<SegurancaOptions>>().Value;
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite.LimiteTentativasLogin,
                    Window = TimeSpan.FromSeconds(limite.JanelaTentativasLoginSegundos),
                    QueueLimit = 0,
                });
            });
            options.OnRejected = (contexto, _) =>
            {
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    contexto.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                return new ValueTask(EscreverFalhaAsync(contexto.HttpContext, StatusCodes.Status429TooManyRequests,
                    CodigosErro.MuitasTentativas, "Muitas tentativas. Aguarde um pouco e tente novamente."));
            };
        });

        return services;
    }

    public static IMvcBuilder AddRespostasPadronizadas(this IMvcBuilder builder)
    {
        builder.AddMvcOptions(options =>
        {
            // Valida o token anti-CSRF em todo POST/PUT/PATCH/DELETE.
            options.Filters.Add<ValidarAntiforgeryFilter>();
        });

        builder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = contexto =>
            {
                var detalhes = contexto.ModelState
                    .Where(campo => campo.Value?.Errors.Count > 0)
                    .ToDictionary(
                        campo => JsonNamingPolicy.CamelCase.ConvertName(campo.Key),
                        campo => campo.Value!.Errors.Select(erro => erro.ErrorMessage).ToArray());

                return new BadRequestObjectResult(ApiResposta.Falha(CodigosErro.DadosInvalidos,
                    "Verifique os dados informados.", detalhes));
            };
        });

        return builder;
    }

    public static IApplicationBuilder UseTratamentoDeErros(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(erro => erro.Run(contexto =>
            EscreverFalhaAsync(contexto, StatusCodes.Status500InternalServerError,
                CodigosErro.ErroInterno, "Ocorreu um erro inesperado. Tente novamente.")));
    }

    private static Task EscreverFalhaAsync(HttpContext contexto, int statusCode, string codigo, string mensagem)
    {
        contexto.Response.StatusCode = statusCode;
        return contexto.Response.WriteAsJsonAsync(ApiResposta.Falha(codigo, mensagem), OpcoesJson);
    }
}
