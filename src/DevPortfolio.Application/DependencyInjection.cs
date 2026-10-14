using DevPortfolio.Application.Auth;
using DevPortfolio.Application.Conta;
using DevPortfolio.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DevPortfolio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<ContaOptions>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IContaService, ContaService>();

        return services;
    }
}
