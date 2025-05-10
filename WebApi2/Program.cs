var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

app.MapGet("/hello", () => "Hello from Api 2");

app.MapGet("/hello2", () => "Hello again from Api 2");

app.Run();