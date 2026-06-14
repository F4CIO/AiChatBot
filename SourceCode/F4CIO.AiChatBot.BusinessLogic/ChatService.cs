using System;
using System.Threading;
using System.Threading.Tasks;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Common.Entities;
using F4CIO.AiChatBot.Contracts;

namespace F4CIO.AiChatBot.BusinessLogic;

/// <summary>
/// The IChatService implementation: orchestrates conversation tracking, conversation logging and the
/// Claude managed-agent calls. Register as a singleton (it holds the in-memory conversation map and
/// the cached Claude resource ids).
/// </summary>
public sealed partial class ChatService : IChatService
{
    private readonly Configuration _cfg;
    private readonly HandlerForClaude _claude;
    private readonly HandlerForConversation _conversations = new();

    public ChatService(Configuration cfg)
    {
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        _claude = new HandlerForClaude(cfg);
    }

    public Task EnsureReadyAsync(CancellationToken ct = default) => _claude.EnsureReadyAsync(ct);

    public AppInfo GetAppInfo() => new() { AppTitle = _cfg.AppTitle, UseAsync = _cfg.UseAsync };

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken ct = default)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Message))
                throw new ArgumentException("Message must not be empty.");

            string conversationId;
            string sessionId;
            DateTime start;

            if (!string.IsNullOrWhiteSpace(request.ConversationId)
                && _conversations.TryGet(request.ConversationId!, out var state)
                && state is not null)
            {
                conversationId = request.ConversationId!;
                sessionId = state.SessionId;
                start = state.Start;
            }
            else
            {
                start = DateTime.Now;
                sessionId = await _claude.StartSessionAsync(ct);
                conversationId = _conversations.Create(sessionId, start);
                HandlerForLogging.AddLine($"New conversation {conversationId} (session {sessionId})");
            }

            _conversations.LogUser(start, request.Message);
            var reply = await _claude.SendAndGetReplyAsync(sessionId, request.Message, ct);
            _conversations.LogAssistant(start, reply);

            return new ChatResponse { ConversationId = conversationId, Reply = reply };
        }
        catch (AppException)
        {
            throw; // already built + logged
        }
        catch (Exception ex)
        {
            // Build the Error POCO, log it (with LogId), and propagate to the UI.
            throw new AppException(HandlerForErrors.Handle(ex));
        }
    }
}
