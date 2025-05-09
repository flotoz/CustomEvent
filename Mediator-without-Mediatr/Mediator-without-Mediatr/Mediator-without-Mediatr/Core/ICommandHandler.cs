namespace Mediator_without_Mediatr.Core;

internal interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command);
}