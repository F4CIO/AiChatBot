namespace F4CIO.AiChatBot.Common.Entities;

/// <summary>
/// Single error carrier used across all layers. Instantiated in HandlerForErrors when an
/// exception is caught, logged (with <see cref="LogId"/>) via the central logger, then propagated
/// to the UI so the user sees <see cref="Message"/> together with the <see cref="LogId"/>.
/// <see cref="Exception"/> and <see cref="StackTrace"/> stay server-side and are not sent to clients.
/// </summary>
public class Error
{
    public string Message { get; set; } = "";
    public string? StackTrace { get; set; }
    public string LogId { get; set; } = "";
    public System.Exception? Exception { get; set; }
}
