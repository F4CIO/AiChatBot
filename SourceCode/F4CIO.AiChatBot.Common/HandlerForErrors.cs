using System.Linq;
using F4CIO.AiChatBot.Common.Entities;

namespace F4CIO.AiChatBot.Common;

/// <summary>
/// Central exception handling. Catches turn an exception into a single <see cref="Error"/> POCO,
/// log it (with its LogId) via <see cref="HandlerForLogging"/>, and return it for propagation to
/// the UI. The LogId is the current timestamp from year to millisecond, digits only
/// (e.g. 20260613091500123) so it can be found quickly in the log file.
/// </summary>
public static class HandlerForErrors
{
    public static string BuildLogId() =>
        new string($"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}".Where(char.IsDigit).ToArray());

    public static Error Handle(System.Exception ex)
    {
        var err = new Error
        {
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            LogId = BuildLogId(),
            Exception = ex,
        };
        HandlerForLogging.AddLine(
            $"ERROR LogId={err.LogId} :: {err.Message}{System.Environment.NewLine}{err.StackTrace}");
        return err;
    }
}
