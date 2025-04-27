using System.Threading.Channels;

namespace Medium.Examples;

public interface IEvent { }

public interface IEventHandler
{
    void Register(IEventBus eventBus);
}

public interface IEventHandler<in TEvent> : IEventHandler
    where TEvent : IEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}

public interface ICommandEvent { }
public interface ICommandEventHandler<in TCommand> : IEventHandler
    where TCommand : ICommandEvent
{
    Task HandleAsync(TCommand @event, CancellationToken cancellationToken = default);
}

public interface IQueryEvent { }
public interface IQueryEventHandler<in TQuery, TResult> : IEventHandler
    where TQuery : IQueryEvent
{
    Task<TResult> HandleAsync(TQuery @event, CancellationToken cancellationToken = default);
}

internal sealed class InMemoryMessageQueue
{
    private readonly Channel<IEvent> _channel = 
        Channel.CreateUnbounded<IEvent>();

    public ChannelReader<IEvent> Reader => _channel.Reader;
    public ChannelWriter<IEvent> Writer => _channel.Writer;
}

public interface IEventBus
{
    Task StartConsumingAsync(CancellationToken cancellationToken = default);
    
    void Register<TCommand>(ICommandEventHandler<TCommand> handler)
        where TCommand : ICommandEvent;

    void Register<TQuery, TResult>(IQueryEventHandler<TQuery, TResult> handler)
        where TQuery : IQueryEvent;

    void Register<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent;

    Task SendCommandAsync<TCommand>(TCommand commandEvent, CancellationToken cancellationToken = default)
        where TCommand : ICommandEvent;

    Task<TResult> SendQueryAsync<TQuery, TResult>(TQuery queryEvent, CancellationToken cancellationToken = default)
        where TQuery : IQueryEvent;

    Task PublishAsync<TEvent>(TEvent eventToPublish, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}

internal sealed class EventBus(InMemoryMessageQueue _queue) : IEventBus
{
    private readonly Dictionary<Type, Func<ICommandEvent, CancellationToken, Task>> _commandHandlers = new();
    private readonly Dictionary<Type, Func<IQueryEvent, CancellationToken, Task<object>>> _queryHandlers = new();
    private readonly Dictionary<Type, List<Func<IEvent, CancellationToken, Task>>> _eventHandlers = new();

    public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var integrationEvent in _queue.Reader.ReadAllAsync(cancellationToken))
        {
            if (!_eventHandlers.TryGetValue(integrationEvent.GetType(), out var handlers))
            {
                continue;
            }

            foreach (var handler in handlers)
            {
                await handler(integrationEvent, cancellationToken);
            }
        }
    }
    
    // Register Command Handlers
    public void Register<TCommand>(ICommandEventHandler<TCommand> handler)
        where TCommand : ICommandEvent
    {
        var commandType = typeof(TCommand);
        _commandHandlers[commandType] = async (command, token) =>
        {
            await handler.HandleAsync((TCommand)command, token);
        };
    }

    // Register Query Handlers
    public void Register<TQuery, TResult>(IQueryEventHandler<TQuery, TResult> handler)
        where TQuery : IQueryEvent
    {
        var queryType = typeof(TQuery);
        _queryHandlers[queryType] = async (query, token) => await handler.HandleAsync((TQuery)query, token);
    }

    // Register Event Handlers (for Publishing Events)
    public void Register<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        if (!_eventHandlers.ContainsKey(eventType))
        {
            _eventHandlers[eventType] = new();
        }

        _eventHandlers[eventType].Add(async (eventToHandle, token) => await handler.HandleAsync((TEvent)eventToHandle, token));
    }

    public async Task SendCommandAsync<TCommand>(TCommand commandEvent, CancellationToken cancellationToken = default)
        where TCommand : ICommandEvent
    {
        if (!_commandHandlers.TryGetValue(commandEvent.GetType(), out var handler))
        {
            throw new ArgumentException($"No handler registered for command '{commandEvent.GetType().Name}'");
        }  
        await handler(commandEvent, cancellationToken);
    }

    public async Task<TResult> SendQueryAsync<TQuery, TResult>(TQuery queryEvent, CancellationToken cancellationToken = default)
        where TQuery : IQueryEvent
    {
        if (!_queryHandlers.TryGetValue(queryEvent.GetType(), out var handler))
        { 
            throw new ArgumentException($"No handler registered for query '{queryEvent.GetType().Name}'"); 
        }
        return (TResult)await handler(queryEvent, cancellationToken);
    }

    public async Task PublishAsync<TEvent>(TEvent eventToPublish, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        if (!_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            throw new ArgumentException($"No handler registered for event '{eventToPublish.GetType().Name}'");
        }
        foreach (var handler in handlers)
        {
            await handler(eventToPublish, cancellationToken);
        }
    }
}

public class EventBusBackgroundService(IEventBus _eventBus, IServiceProvider _serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var eventHandlers = scope.ServiceProvider.GetServices<IEventHandler>();
        
        foreach (var eventHandler in eventHandlers)
        {
            eventHandler.Register(_eventBus);
        }
        await _eventBus.StartConsumingAsync(stoppingToken);
    }
}

public class UserCreatedHandler : IEventHandler<UserCreatedHandler.UserCreatedEvent>
{
    public record UserCreatedEvent(Guid UserId) : IEvent;
    public void Register(IEventBus eventBus)
    {
        eventBus.Register(this);
    }

    public Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"User created: {@event.UserId}");
        return Task.CompletedTask;
    }
}
