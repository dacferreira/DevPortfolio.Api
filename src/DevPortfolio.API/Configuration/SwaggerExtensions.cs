using System.Reflection;
using Microsoft.OpenApi.Models;

namespace DevPortfolio.API.Configuration;

public static class SwaggerExtensions
{
    private const string EsquemaCsrf = "Csrf";

    public static IServiceCollection AddSwaggerDocumentado(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DevPortfolio API",
                Version = "v1",
                Description =
                    "API do DevPortfolio.\n\n" +
                    "**Autenticação:** a sessão fica em um cookie HttpOnly emitido por `POST /api/auth/login`.\n\n" +
                    "**CSRF:** requisições POST/PUT/PATCH/DELETE exigem o header `X-XSRF-TOKEN`. " +
                    "Chame `GET /api/auth/csrf`, copie o valor do cookie `XSRF-TOKEN` e informe-o em *Authorize*. " +
                    "Depois do login e do logout o token muda: chame `GET /api/auth/csrf` de novo.",
            });

            options.AddSecurityDefinition(EsquemaCsrf, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = SegurancaExtensions.HeaderTokenCsrf,
                Description = "Valor do cookie XSRF-TOKEN.",
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = EsquemaCsrf },
                }] = Array.Empty<string>(),
            });

            // Comentários XML da API e dos DTOs da Application.
            foreach (var assembly in new[] { Assembly.GetExecutingAssembly(), typeof(Application.DependencyInjection).Assembly })
            {
                var arquivoXml = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(arquivoXml))
                    options.IncludeXmlComments(arquivoXml);
            }
        });

        return services;
    }
}
