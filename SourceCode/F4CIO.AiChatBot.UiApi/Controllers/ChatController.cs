using System;
using System.Threading;
using System.Threading.Tasks;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Common.Entities;
using F4CIO.AiChatBot.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace F4CIO.AiChatBot.UiApi.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chat;

    public ChatController(IChatService chat) => _chat = chat;

    /// <summary>App metadata (e.g. the configured title) for the UI.</summary>
    [HttpGet("app-info")]
    [ProducesResponseType(typeof(AppInfo), StatusCodes.Status200OK)]
    public ActionResult<AppInfo> GetAppInfo() => _chat.GetAppInfo();

    /// <summary>Send a chat message and receive the assistant's full reply.</summary>
    [HttpPost("messages")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorInfo), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request, CancellationToken ct)
        => await _chat.SendMessageAsync(request, ct);

    /// <summary>
    /// Send a chat message and receive the assistant's reply as a stream of Server-Sent Events:
    /// a <c>meta</c> frame (conversationId), then <c>chunk</c> frames as the reply is produced, then a
    /// <c>done</c> frame — or an <c>error</c> frame. Hand-consumed by the SPA (kept out of the generated
    /// OpenAPI client) and active when <c>UseAsync</c> is enabled.
    /// </summary>
    [HttpPost("messages/stream")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task SendMessageStream([FromBody] ChatRequest request, CancellationToken ct)
    {
        var sse = new HandlerForServerSentEvents(Response);
        try
        {
            await foreach (var chunk in _chat.StreamMessageAsync(request, ct))
            {
                if (chunk.Error is not null)
                    await sse.WriteAsync("error", chunk.Error, ct);
                else if (chunk.ConversationId is not null)
                    await sse.WriteAsync("meta", new { conversationId = chunk.ConversationId }, ct);
                else
                    await sse.WriteAsync("chunk", new { text = chunk.Text }, ct);
            }
            await sse.WriteAsync("done", new { }, ct);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected / request aborted — nothing to report.
        }
        catch (AppException appEx)
        {
            await sse.WriteAsync("error", new ErrorInfo { Message = appEx.Error.Message, LogId = appEx.Error.LogId }, ct);
        }
        catch (Exception ex)
        {
            var err = HandlerForErrors.Handle(ex);
            await sse.WriteAsync("error", new ErrorInfo { Message = err.Message, LogId = err.LogId }, ct);
        }
    }
}
