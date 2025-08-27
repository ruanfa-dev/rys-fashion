using Infrastructure;

using Serilog;

using SharedKernel.Logging;

using UseCases;

using Web.Api.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Services.AddUseCases();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddPresentation(builder.Configuration);

var app = builder.Build();

app.UseUseCases();
app.UseInfrastructure(app.Environment);
app.UsePresentation();

Log.Information(LogTemplate.ComponentStarted, "Application");

app.Run();
