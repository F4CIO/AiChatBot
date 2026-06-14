namespace F4CIO.AiChatBot.Common;

/// <summary>
/// Resolves the on-disk "root" folder for log/conversation files using standard per-host
/// techniques (no reliance on a .csproj, which does not exist in production).
/// The default — <see cref="System.AppContext.BaseDirectory"/> — is correct for a console app or
/// a class library (the folder the executing assembly runs from). A web host overrides it once at
/// startup with its ContentRootPath via <see cref="SetRoot"/>.
/// </summary>
public static class HandlerForPaths
{
    private static string _root = System.AppContext.BaseDirectory;

    /// <summary>Called once at host startup. Web app: pass IHostEnvironment.ContentRootPath.</summary>
    public static void SetRoot(string root)
    {
        if (!string.IsNullOrWhiteSpace(root)) _root = root;
    }

    /// <summary>Root folder for the host's F4CIO.AiChatBot.log file.</summary>
    public static string Root => _root;

    /// <summary>
    /// Root for the BusinessLogic-scoped Conversations folder. BusinessLogic ships as a DLL in the
    /// host's output folder, so at runtime this is the executing application's base directory.
    /// </summary>
    public static string BusinessLogicRoot => System.AppContext.BaseDirectory;
}
