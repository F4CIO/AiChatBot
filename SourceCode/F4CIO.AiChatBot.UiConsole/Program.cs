using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using F4CIO.AiChatBot.BusinessLogic;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Common.Entities;
using F4CIO.AiChatBot.Contracts;

// Logs go to F4CIO.AiChatBot.log next to this console app.
HandlerForPaths.SetRoot(AppContext.BaseDirectory);

var config = LoadConfiguration();
if (string.IsNullOrWhiteSpace(config.ClaudeApiKey))
{
    Console.Write("Claude API key (leave blank to abort): ");
    config.ClaudeApiKey = (Console.ReadLine() ?? "").Trim();
    if (string.IsNullOrWhiteSpace(config.ClaudeApiKey)) return;
}

IChatService chat = new ChatService(config);

Console.WriteLine($"=== {config.AppTitle} (console) ===");
Console.WriteLine("Preparing Claude (environment / agent / memory store)...");
try
{
    await chat.EnsureReadyAsync();
}
catch (Exception ex)
{
    var e = HandlerForErrors.Handle(ex);
    Console.WriteLine($"Startup error: {e.Message} (LogId: {e.LogId})");
}

Console.WriteLine("Type a message and press Enter. Blank line or 'exit' quits.\n");

string? conversationId = null;
while (true)
{
    Console.Write("you> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    try
    {
        if (config.UseAsync)
        {
            // Async path: print the reply chunk-by-chunk as it streams in.
            conversationId = await RunStreamingTurnAsync(chat, conversationId, input);
        }
        else
        {
            var resp = await chat.SendMessageAsync(new ChatRequest { ConversationId = conversationId, Message = input });
            conversationId = resp.ConversationId;
            Console.WriteLine($"bot> {resp.Reply}\n");
        }
    }
    catch (AppException ax)
    {
        Console.WriteLine($"error> {ax.Error.Message} (LogId: {ax.Error.LogId})\n");
    }
    catch (Exception ex)
    {
        var e = HandlerForErrors.Handle(ex);
        Console.WriteLine($"error> {e.Message} (LogId: {e.LogId})\n");
    }
}

Console.WriteLine("Bye.");

// Streaming chat turn: write the reply piece-by-piece as it arrives; return the conversation id.
static async Task<string?> RunStreamingTurnAsync(IChatService chat, string? conversationId, string input)
{
    var newConversationId = conversationId;
    Console.Write("bot> ");
    await foreach (var chunk in chat.StreamMessageAsync(new ChatRequest { ConversationId = conversationId, Message = input }))
    {
        if (chunk.Error is not null)
        {
            Console.WriteLine();
            Console.WriteLine($"error> {chunk.Error.Message} (LogId: {chunk.Error.LogId})");
        }
        else if (chunk.ConversationId is not null)
        {
            newConversationId = chunk.ConversationId;
        }
        else
        {
            Console.Write(chunk.Text);
        }
    }
    Console.WriteLine("\n");
    return newConversationId;
}

static Configuration LoadConfiguration()
{
    try
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(path))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("Configuration", out var section))
            {
                var cfg = section.Deserialize<Configuration>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (cfg is not null) return cfg;
            }
        }
    }
    catch
    {
        // fall through to defaults
    }
    return new Configuration();
}
