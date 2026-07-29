using F4CIO.AiChatBot.BusinessLogic;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Common.Entities;
using F4CIO.AiChatBot.Contracts;
using F4CIO.AiChatBot.UiHtmx;

var builder = WebApplication.CreateBuilder(args);

// Logs go to F4CIO.AiChatBot.log at this web host's content root.
HandlerForPaths.SetRoot(builder.Environment.ContentRootPath);

// Bind the single operator-supplied Configuration POCO and share it.
var config = builder.Configuration.GetSection("Configuration").Get<Configuration>() ?? new Configuration();
builder.Services.AddSingleton(config);

// One chat service for the app (holds the in-memory conversation map + cached Claude ids).
builder.Services.AddSingleton<IChatService>(_ => new ChatService(config));

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// Create the Claude environment/agent/memory store once at startup.
builder.Services.AddHostedService<HandlerForStartup>();

var app = builder.Build();

// Central API error handling -> { message, logId } as JSON.
app.UseMiddleware<HandlerForApiExceptions>();

app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapRazorPages();

app.Run();
