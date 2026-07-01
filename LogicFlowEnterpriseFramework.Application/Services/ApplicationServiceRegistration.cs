using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace LogicFlowEnterpriseFramework.Application.Services;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
        return services;
    }
}
