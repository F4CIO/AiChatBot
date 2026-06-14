using F4CIO.AiChatBot.Common.Entities;

namespace F4CIO.AiChatBot.Common;

/// <summary>
/// Exception that carries an already-built, already-logged <see cref="Error"/> up to a UI host.
/// The host maps it to a user-facing payload (message + LogId) without re-logging.
/// </summary>
public sealed class AppException : System.Exception
{
    public Error Error { get; }

    public AppException(Error error) : base(error.Message) => Error = error;
}
