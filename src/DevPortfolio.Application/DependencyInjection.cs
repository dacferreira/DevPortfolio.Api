using DevPortfolio.Application.Auth;
using DevPortfolio.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DevPortfolio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
