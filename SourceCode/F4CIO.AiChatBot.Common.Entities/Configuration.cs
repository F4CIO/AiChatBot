namespace F4CIO.AiChatBot.Common.Entities;

/// <summary>
/// Single carrier for every operator-supplied setting. Each executable host (UiApi, UiConsole)
/// binds this from its own appsettings.json and passes it into the business logic. The
/// BusinessLogic library never reads a config file itself — it only receives this object.
/// </summary>
public class Configuration
{
    /// <summary>Title shown in the chatbot UI (served to the SPA via the API).</summary>
    public string AppTitle { get; set; } = "F4CIO AI Chatbot";

    /// <summary>When true, chat replies stream to the UI progressively as they are produced; when false, the full reply is returned in one response.</summary>
    public bool UseAsync { get; set; } = true;

    /// <summary>When streaming, split each Claude message into sentence/paragraph-sized chunks for smoother progressive rendering. No effect when <see cref="UseAsync"/> is false.</summary>
    public bool UseSentenceChunking { get; set; } = true;

    /// <summary>Pause (milliseconds) inserted after each sentence chunk to produce a visible typing cadence. 0 disables pacing. Applies only when <see cref="UseAsync"/> and <see cref="UseSentenceChunking"/> are true.</summary>
    public int AddSentenceChunkingDelayInMs { get; set; } = 50;

    /// <summary>Claude API credentials (the x-api-key value).</summary>
    public string ClaudeApiKey { get; set; } = "";

    /// <summary>Claude API base URL endpoint.</summary>
    public string ClaudeBaseUrl { get; set; } = "https://api.anthropic.com";

    /// <summary>Name of the Claude managed Agent (found-or-created at startup).</summary>
    public string ClaudeAgentName { get; set; } = "F4CIO-Chatbot-Agent";

    /// <summary>Name of the Claude Memory store (found-or-created at startup, attached per session).</summary>
    public string ClaudeMemoryStoreName { get; set; } = "F4CIO-Chatbot-Memory";

    /// <summary>Name of the Claude managed-agents Environment (found-or-created at startup).</summary>
    public string ClaudeEnvironmentName { get; set; } = "F4CIO-Chatbot-Env";

    /// <summary>Claude model id used by the agent.</summary>
    public string ClaudeModel { get; set; } = "claude-opus-4-8";

    /// <summary>Backend API base URL — used for CORS (the SPA origin) and tooling.</summary>
    public string ApiBaseUrl { get; set; } = "";
}
