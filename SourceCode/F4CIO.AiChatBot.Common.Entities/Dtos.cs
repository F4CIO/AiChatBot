namespace F4CIO.AiChatBot.Common.Entities;

/// <summary>A chat turn sent from a UI to the backend.</summary>
public class ChatRequest
{
    /// <summary>Opaque conversation id returned by a previous response; null/empty starts a new conversation.</summary>
    public string? ConversationId { get; set; }

    /// <summary>The user's message text.</summary>
    public string Message { get; set; } = "";
}

/// <summary>The assistant reply returned to a UI.</summary>
public class ChatResponse
{
    public string ConversationId { get; set; } = "";
    public string Reply { get; set; } = "";
}

/// <summary>Lightweight app metadata the SPA fetches (e.g. the configured title).</summary>
public class AppInfo
{
    public string AppTitle { get; set; } = "";

    /// <summary>True when the backend streams replies progressively; the SPA then uses the streaming endpoint.</summary>
    public bool UseAsync { get; set; }
}

/// <summary>Error payload shown to the user: a message plus the LogId to quote when reporting it.</summary>
public class ErrorInfo
{
    public string Message { get; set; } = "";
    public string LogId { get; set; } = "";
}
