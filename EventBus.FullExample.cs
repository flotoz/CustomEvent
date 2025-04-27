namespace FloToz.Namespace;

public interface IEvent { }
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

public interface ICommandEvent { }
public interface ICommandEventHandler<in TCommand>
    where TCommand : ICommandEvent
{
    Task HandleAsync(TCommand @event, CancellationToken cancellationToken = default);
}

public interface IQueryEvent { }
public interface IQueryEventHandler<in TQuery, TResult>
    where TQuery : IQueryEvent
{
    Task<TResult> HandleAsync(TQuery @event, CancellationToken cancellationToken = default);
}


public interface IEventBus
{
    // Register Command Handlers
    void RegisterCommandHandler<TCommand>(ICommandEventHandler<TCommand> handler)
        where TCommand : ICommandEvent;

    // Register Query Handlers
    void RegisterQueryHandler<TQuery, TResult>(IQueryEventHandler<TQuery, TResult> handler)
        where TQuery : IQueryEvent;

    // Publish Event Handlers
    void RegisterEventHandler<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent;

    // Send Command (Async)
    Task SendCommandAsync<TCommand>(TCommand commandEvent, CancellationToken cancellationToken = default)
        where TCommand : ICommandEvent;

    // Send Query (Async) and return result
    Task<TResult> SendQueryAsync<TQuery, TResult>(TQuery queryEvent, CancellationToken cancellationToken = default)
        where TQuery : IQueryEvent;

    // Publish Event (Async)
    Task PublishAsync<TEvent>(TEvent eventToPublish, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}


internal sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, Func<ICommandEvent, CancellationToken, Task>> _commandHandlers = new();
    private readonly Dictionary<Type, Func<IQueryEvent, CancellationToken, Task<object>>> _queryHandlers = new();
    private readonly Dictionary<Type, List<Func<IEvent, CancellationToken, Task>>> _eventHandlers = new();

    // Register Command Handlers
    public void RegisterCommandHandler<TCommand>(ICommandEventHandler<TCommand> handler)
        where TCommand : ICommandEvent
    {
        var commandType = typeof(TCommand);
        _commandHandlers[commandType] = async (command, token) =>
        {
            await handler.HandleAsync((TCommand)command, token);
        };
    }

    // Register Query Handlers
    public void RegisterQueryHandler<TQuery, TResult>(IQueryEventHandler<TQuery, TResult> handler)
        where TQuery : IQueryEvent
    {
        var queryType = typeof(TQuery);
        _queryHandlers[queryType] = async (query, token) =>
        {
            return await handler.HandleAsync((TQuery)query, token);
        };
    }

    // Register Event Handlers (for Publishing Events)
    public void RegisterEventHandler<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        if (!_eventHandlers.ContainsKey(eventType))
        {
            _eventHandlers[eventType] = new List<Func<IEvent, CancellationToken, Task>>();
        }

        _eventHandlers[eventType].Add(async (eventToHandle, token) =>
        {
            await handler.HandleAsync((TEvent)eventToHandle, token);
        });
    }

    public async Task SendCommandAsync<TCommand>(TCommand commandEvent, CancellationToken cancellationToken = default)
        where TCommand : ICommandEvent
    {
        if (_commandHandlers.TryGetValue(commandEvent.GetType(), out var handler))
        {
            await handler(commandEvent, cancellationToken);
        }
        else
        {
            throw new ArgumentException($"No handler registered for command '{commandEvent.GetType().Name}'");
        }
    }

    public async Task<TResult> SendQueryAsync<TQuery, TResult>(TQuery queryEvent, CancellationToken cancellationToken = default)
        where TQuery : IQueryEvent
    {
        if (_queryHandlers.TryGetValue(queryEvent.GetType(), out var handler))
        {
            return (TResult)await handler(queryEvent, cancellationToken);
        }
        else
        {
            throw new ArgumentException($"No handler registered for query '{queryEvent.GetType().Name}'");
        }
    }

    public async Task PublishAsync<TEvent>(TEvent eventToPublish, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                await handler(eventToPublish, cancellationToken);
            }
        }
        else
        {
            throw new ArgumentException($"No handler registered for event '{eventToPublish.GetType().Name}'");
        }
    }
}
