using System.IO.Abstractions;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
// Makes our controllers available for dependency injection. This is used behind the scenes by Asp.Net Core.
builder.Services.AddControllers();

// if anybody asks for a IFileSystem, they will be given this concrete implementation FileSystem.
builder.Services.AddTransient<IFileSystem, FileSystem>();

// Configures the JsonSerializer when we read/write Json files.
builder.Services.AddTransient(_ => new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
});

var config = builder.Configuration;

builder.Services.AddCors(cfg =>
{
    cfg.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyOrigin()
              .AllowAnyMethod();
    });
});

// Builds our web application. If not done, nothing will be listening for HTTP requests.
var app = builder.Build();

// This tells Asp.Net Core to build a route map based on the route attributes defined in our controllers.
// If we don't do this, we have a HTTP server but requests won't go anywhere.
app.MapControllers();

app.UseCors();
// app.UseCors(cfg => cfg.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.Run();
