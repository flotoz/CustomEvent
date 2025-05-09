using Mediator_without_Mediatr.Core;
using Mediator_without_Mediatr.CreateUser;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi()
    .RegisterCQRSDispatcherAndHandlers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/test", (IDispatcher dispatcher) =>
    {
       dispatcher.SendAsync(new CreateUser("John", "john@example.com"));
    })
    .WithName("GetTest");

app.Run();
