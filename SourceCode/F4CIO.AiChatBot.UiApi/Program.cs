using F4CIO.AiChatBot.BusinessLogic;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Common.Entities;
using F4CIO.AiChatBot.Contracts;
using F4CIO.AiChatBot.UiApi;

var builder = WebApplication.CreateBuilder(args);
// Logs go to F4CIO.AiChatBot.log at this web host's content root.
HandlerForPaths.SetRoot(builder.Environment.ContentRootPath);

// Bind the single operator-supplied Configuration POCO and share it.
var config = builder.Configuration.GetSection("Configuration").Get<Configuration>() ?? new Configuration();
builder.Services.AddSingleton(config);

// One chat service for the app (holds the in-memory conversation map + cached Claude ids).
builder.Services.AddSingleton<IChatService>(_ => new ChatService(config));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for the React SPA origin(s).
const string CorsPolicy = "spa";
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? new[] { "http://localhost:5101", "http://localhost:5102" };
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy,
    p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

// Create the Claude environment/agent/memory store once at startup.
builder.Services.AddHostedService<HandlerForStartup>();

var app = builder.Build();

// Central API error handling -> { message, logId } as JSON.
app.UseMiddleware<HandlerForApiExceptions>();

// Swagger JSON + UI (the OpenAPI doc at /swagger/v1/swagger.json feeds NSwag client generation).
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(CorsPolicy);
app.MapControllers();

app.Run();
