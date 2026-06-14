using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using F4CIO.AiChatBot.Common.Entities;

namespace F4CIO.AiChatBot.Contracts;

/// <summary>
/// The chatbot operation contract. Implemented by BusinessLogic, exposed over REST by UiApi, called
/// directly by UiConsole, and (via the generated OpenAPI client) consumed by the React UiWeb.
/// </summary>
public interface IChatService
{
    /// <summary>Ensure the Claude environment, agent and memory store exist. Run once at startup.</summary>
    Task EnsureReadyAsync(CancellationToken ct = default);

    /// <summary>Send a user message and get the assistant's full reply (synchronous request/reply).</summary>
    Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken ct = default);

    /// <summary>Send a user message and stream the assistant's reply in chunks as it is produced (async path).</summary>
    IAsyncEnumerable<ChatStreamChunk> StreamMessageAsync(ChatRequest request, CancellationToken ct = default);

    /// <summary>Configured app metadata (e.g. the title) for the UI.</summary>
    AppInfo GetAppInfo();
}
