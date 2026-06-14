namespace F4CIO.AiChatBot.Common;

/// <summary>
/// Central logging. Every layer calls the single method <see cref="AddLine"/>, which writes to the
/// debug output and appends to F4CIO.AiChatBot.log at the running host's root folder
/// (see <see cref="HandlerForPaths"/>). Logging never throws.
/// </summary>
public static class HandlerForLogging
{
    private static readonly object _gate = new();

    public static void AddLine(string log)
    {
        System.Diagnostics.Debug.WriteLine(log);
        try
        {
            var path = System.IO.Path.Combine(HandlerForPaths.Root, "F4CIO.AiChatBot.log");
            var line = $"{System.DateTime.Now:yyyyMMddHHmmssfff}\t{log}{System.Environment.NewLine}";
            lock (_gate) System.IO.File.AppendAllText(path, line);
        }
        catch
        {
            // Logging must never break the caller.
        }
    }
}
