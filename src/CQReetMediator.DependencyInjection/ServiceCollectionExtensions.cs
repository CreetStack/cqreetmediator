using System.Reflection;
using CQReetMediator.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CQReetMediator.DependencyInjection;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddCQReetMediator(this IServiceCollection services, params Assembly[] assemblies) {
        services.AddSingleton<HandlerRegistry>();
        services.AddSingleton<PipelineExecutor>();
        services.AddSingleton<NotificationPublisher>();
        services.AddSingleton<IMediator, Mediator>();

        RegisterHandlers(services, assemblies);
        RegisterNotificationHandlers(services, assemblies);
        RegisterPipelineBehaviors(services, assemblies);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies) {
        var handlers = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces()
                .Where(i =>
                    i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .Select(i => new { Handler = t, Interface = i }));

        foreach (var h in handlers)
            services.AddScoped(h.Interface, h.Handler);
    }

    private static void RegisterNotificationHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        var handlers = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces()
                .Where(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .Select(i => new { Handler = t, Interface = i }));

        foreach (var h in handlers)
            services.AddScoped(h.Interface, h.Handler);
    }


    private static void RegisterPipelineBehaviors(IServiceCollection services, Assembly[] assemblies)
    {
        var behaviors = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                !t.IsInterface &&
                !t.IsAbstract &&
                typeof(IPipelineBehavior).IsAssignableFrom(t));

        foreach (var b in behaviors)
            services.AddScoped(typeof(IPipelineBehavior), b);
    }
}