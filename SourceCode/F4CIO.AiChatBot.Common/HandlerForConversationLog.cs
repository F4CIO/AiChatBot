namespace F4CIO.AiChatBot.Common;

/// <summary>
/// Central conversation logging. Each conversation is written to its own file
/// F4CIO.AiChatBot.Conversation{start}.txt under a Conversations sub-folder next to the
/// BusinessLogic assembly. The file is keyed by the conversation's start timestamp, which the
/// caller publishes into <see cref="CurrentStart"/> (an AsyncLocal, so it is correct under
/// concurrent web requests) before invoking <see cref="AddMessage"/>.
/// </summary>
public static class HandlerForConversationLog
{
    /// <summary>Start timestamp of the conversation currently being logged on this async flow.</summary>
    public static readonly System.Threading.AsyncLocal<System.DateTime> CurrentStart = new();

    private static readonly object _gate = new();

    public static void AddMessage(System.DateTime moment, string role, string body)
    {
        string m = $"{moment:yyyyMMddHHmmss} [{role}] {body}";
        System.Diagnostics.Debug.WriteLine(m);
        try
        {
            var dir = System.IO.Path.Combine(HandlerForPaths.BusinessLogicRoot, "Conversations");
            System.IO.Directory.CreateDirectory(dir);
            var start = CurrentStart.Value == default ? moment : CurrentStart.Value;
            var file = $"F4CIO.AiChatBot.Conversation{start:yyyyMMddHHmmss}.txt";
            lock (_gate)
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, file), m + System.Environment.NewLine);
        }
        catch
        {
            // Conversation logging must never break the caller.
        }
    }
}
