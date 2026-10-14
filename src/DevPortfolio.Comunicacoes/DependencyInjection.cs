using DevPortfolio.Application.Interfaces;
using DevPortfolio.Comunicacoes.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Resend;

namespace DevPortfolio.Comunicacoes;

public static class DependencyInjection
{
    // As opções (EmailOptions) são configuradas por quem chama, a partir da seção "Email".
    public static IServiceCollection AddComunicacoes(this IServiceCollection services)
    {
        services.AddResend(_ => { });
        services.AddOptions<ResendClientOptions>()
            .Configure<IOptions<EmailOptions>>((resend, email) => resend.ApiToken = email.Value.ResendApiKey ?? string.Empty);

        services.AddSingleton<FilaEmail>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddHostedService<EnvioEmailWorker>();

        return services;
    }
}
