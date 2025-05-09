namespace Mediator_without_Mediatr.Core;

internal interface IDispatcher
{
    /// <summary>
    /// Sends a command that changes the system's state.
    /// </summary>
    Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand;

    /// <summary>
    /// Sends a query that retrieves data without modifying system state.
    /// </summary>
    Task<TResult> SendAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;
}

internal sealed class Dispatcher(IServiceProvider _provider) : IDispatcher
{
    /// <inherits />
    public async Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        var handler = _provider.GetService<ICommandHandler<TCommand>>()
                      ?? throw new InvalidOperationException($"No handler for {typeof(TCommand).Name}");
        await handler.HandleAsync(command).ConfigureAwait(false);
    }
    /// <inherits />
    public async Task<TResult> SendAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        var handler = _provider.GetService<IQueryHandler<TQuery, TResult>>()
                      ?? throw new InvalidOperationException($"No handler for {typeof(TQuery).Name}");
        return await handler.HandleAsync(query).ConfigureAwait(false);
    }
}