using Mediator_without_Mediatr.Core;

namespace Mediator_without_Mediatr.CreateUser;
internal sealed record CreateUser(string Name, string Email)
    : ICommand;

internal sealed class CreateUserHandler() : ICommandHandler<CreateUser>
{
    public Task HandleAsync(CreateUser command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Console.WriteLine($"Creating user {command.Name} with email {command.Email}");
        return Task.CompletedTask;
    }
}