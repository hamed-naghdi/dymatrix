// Look at: https://github.com/jasontaylordev/CleanArchitecture/blob/main/src/Application/DependencyInjection.cs

using System.Reflection;
using DyMatrix.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace DyMatrix.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}