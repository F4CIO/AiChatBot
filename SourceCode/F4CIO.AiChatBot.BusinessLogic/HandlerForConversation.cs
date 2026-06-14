using System;
using System.Collections.Concurrent;
using F4CIO.AiChatBot.Common;

namespace F4CIO.AiChatBot.BusinessLogic;

/// <summary>State we keep (in memory only — no DB) for one conversation.</summary>
internal sealed class ConversationState
{
    public required string SessionId { get; init; }
    public DateTime Start { get; init; }
}

/// <summary>
/// Tracks conversations in memory (conversation id -&gt; Claude session id + start time) and writes
/// each message through the central conversation logger, keyed by the conversation start.
/// </summary>
internal sealed class HandlerForConversation
{
    private readonly ConcurrentDictionary<string, ConversationState> _map = new();

    public bool TryGet(string conversationId, out ConversationState? state) =>
        _map.TryGetValue(conversationId, out state);

    public string Create(string sessionId, DateTime start)
    {
        var id = Guid.NewGuid().ToString("N");
        _map[id] = new ConversationState { SessionId = sessionId, Start = start };
        return id;
    }

    public void LogUser(DateTime start, string body) => Log(start, "user", body);

    public void LogAssistant(DateTime start, string body) => Log(start, "assistant", body);

    private static void Log(DateTime start, string role, string body)
    {
        // Publish the conversation start onto this async flow so the log file name is correct,
        // then write the line stamped with the actual moment.
        HandlerForConversationLog.CurrentStart.Value = start;
        HandlerForConversationLog.AddMessage(DateTime.Now, role, body);
    }
}
