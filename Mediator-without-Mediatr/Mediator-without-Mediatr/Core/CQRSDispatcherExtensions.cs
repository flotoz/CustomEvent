using System.Reflection;
using Mediator_without_Mediatr.CreateUser;

namespace Mediator_without_Mediatr.Core;

internal static class CQRSDispatcherExtensions
{
    public static IServiceCollection RegisterCQRSDispatcherAndHandlers(this IServiceCollection services)
    {
        // Register the dispatcher
        services.AddScoped<IDispatcher, Dispatcher>();
        
        // Register your command/query handlers here manually, Example:
        services.AddTransient<ICommandHandler<CreateUser.CreateUser>, CreateUserHandler>();

        return services;
    }
    
    public static IServiceCollection AutoRegisterCQRSDispatcherAndHandlers<TApplication>(this IServiceCollection services)
{
    // Register the dispatcher
    services.AddScoped<IDispatcher, Dispatcher>();

    // Get the assembly containing TApplication
    var assembly = typeof(TApplication).Assembly;
    
    // Find and register all command handlers
    RegisterCommandHandlers(services, assembly);
    
    // Find and register all query handlers
    RegisterQueryHandlers(services, assembly);
    
    return services;
}

private static void RegisterCommandHandlers(IServiceCollection services, Assembly assembly)
{
    // Get the open generic type for ICommandHandler<>
    var commandHandlerType = typeof(ICommandHandler<>);
    
    // Find all types in the assembly that implement ICommandHandler<>
    var commandHandlerTypes = assembly.GetTypes()
        .Where(t => !t.IsAbstract && !t.IsInterface)
        .Where(t => t.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == commandHandlerType));
    
    foreach (var handlerType in commandHandlerTypes)
    {
        // Get all interfaces that this type implements which are ICommandHandler<>
        var handlerInterfaces = handlerType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == commandHandlerType);
        
        foreach (var handlerInterface in handlerInterfaces)
        {
            // Register the handler with its interface
            services.AddScoped(handlerInterface, handlerType);
        }
    }
}

private static void RegisterQueryHandlers(IServiceCollection services, Assembly assembly)
{
    // Get the open generic type for IQueryHandler<,>
    var queryHandlerType = typeof(IQueryHandler<,>);
    
    // Find all types in the assembly that implement IQueryHandler<,>
    var queryHandlerTypes = assembly.GetTypes()
        .Where(t => !t.IsAbstract && !t.IsInterface)
        .Where(t => t.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == queryHandlerType));
    
    foreach (var handlerType in queryHandlerTypes)
    {
        // Get all interfaces that this type implements which are IQueryHandler<,>
        var handlerInterfaces = handlerType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == queryHandlerType);
        
        foreach (var handlerInterface in handlerInterfaces)
        {
            // Register the handler with its interface
            services.AddScoped(handlerInterface, handlerType);
        }
    }
}
}
