using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Common.Entities;

namespace F4CIO.AiChatBot.BusinessLogic;

/// <summary>
/// Async (streaming) half of <see cref="ChatService"/>. It mirrors <see cref="ChatService.SendMessageAsync"/>
/// but yields the reply in chunks as it is produced. It reuses the very same Claude handler, session and
/// conversation map as the sync path, so the memory store, conversation lifecycle and logging behave
/// identically — the only difference is that the reply is delivered progressively.
/// </summary>
public sealed partial class ChatService
{
    public async IAsyncEnumerable<ChatStreamChunk> StreamMessageAsync(
        ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // --- Resolve the conversation + session. A failure here becomes a single error chunk. ---
        string conversationId = "";
        string sessionId = "";
        DateTime start = default;
        ErrorInfo? setupError = null;
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Message))
                throw new ArgumentException("Message must not be empty.");

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

            _conversations.LogUser(start, request!.Message);
        }
        catch (Exception ex)
        {
            setupError = ToErrorInfo(ex);
        }

        // `yield` is not allowed inside a catch clause, so surface any setup failure here.
        if (setupError is not null)
        {
            yield return new ChatStreamChunk { Error = setupError };
            yield break;
        }

        // Hand the conversation id back first so the UI can continue this conversation on the next turn.
        yield return new ChatStreamChunk { ConversationId = conversationId };

        // --- Stream the reply. Enumerate manually so a mid-stream failure can be turned into an error ---
        // --- chunk (C# does not allow `yield` inside a try/catch).                                    ---
        var full = new StringBuilder();
        await using var pieces = _claude.StreamReplyAsync(sessionId, request!.Message, ct).GetAsyncEnumerator(ct);
        while (true)
        {
            string piece;
            ErrorInfo? error = null;
            try
            {
                if (!await pieces.MoveNextAsync()) break;
                piece = pieces.Current;
            }
            catch (Exception ex)
            {
                error = ToErrorInfo(ex);
                piece = "";
            }

            if (error is not null)
            {
                yield return new ChatStreamChunk { Error = error };
                yield break;
            }

            full.Append(piece);
            yield return new ChatStreamChunk { Text = piece };
        }

        _conversations.LogAssistant(start, full.ToString().Trim());
    }

    /// <summary>Build, log (with a LogId) and shape an exception into the user-facing error payload.</summary>
    private static ErrorInfo ToErrorInfo(Exception ex)
    {
        var err = HandlerForErrors.Handle(ex);
        return new ErrorInfo { Message = err.Message, LogId = err.LogId };
    }
}
