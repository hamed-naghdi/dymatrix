namespace DyMatrix.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();
        
        return services;
    }
}