using System.Text.Json.Serialization;
using FluentValidation;
using Scalar.AspNetCore;
using TrainingApp.Api.Behaviors;
using TrainingApp.Api.Common;
using TrainingApp.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Controllers are discovered in the whole assembly, so a controller in a feature folder works too.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);

// Strategies: every IDeliveryStrategy<TCommand> in this assembly; the command type uniquely determines the implementation.
builder.Services.AddDeliveryStrategies(typeof(Program).Assembly);

// Handlers and validators are discovered in this assembly as well: a new feature needs no registration here.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.DisableAgent();
        options.DisableMcp();
    });
}

app.MapControllers();

app.Run();
