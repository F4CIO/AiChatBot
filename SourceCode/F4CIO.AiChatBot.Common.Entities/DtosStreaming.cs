namespace F4CIO.AiChatBot.Common.Entities;

/// <summary>
/// One item in a streamed chat reply. Exactly one aspect is meaningful per chunk:
/// the first chunk of a turn carries <see cref="ConversationId"/>; each reply chunk carries
/// <see cref="Text"/>; a failed turn carries <see cref="Error"/> (always the last chunk).
/// </summary>
public class ChatStreamChunk
{
    /// <summary>Set only on the first chunk of a turn — the conversation id to send with the next message.</summary>
    public string? ConversationId { get; set; }

    /// <summary>A piece of the assistant's reply, to append to the chat as it arrives.</summary>
    public string Text { get; set; } = "";

    /// <summary>Set only when the turn failed; carries the user-facing message and the LogId to quote.</summary>
    public ErrorInfo? Error { get; set; }
}
