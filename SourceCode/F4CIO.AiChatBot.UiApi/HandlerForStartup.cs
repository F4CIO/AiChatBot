using System;
using System.Threading;
using System.Threading.Tasks;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Contracts;
using Microsoft.Extensions.Hosting;

namespace F4CIO.AiChatBot.UiApi;

/// <summary>
/// Runs once at startup to ensure the Claude environment, agent and memory store exist. Failures are
/// logged but do not crash the host — the first chat request will retry the setup.
/// </summary>
public sealed class HandlerForStartup : IHostedService
{
    private readonly IChatService _chat;

    public HandlerForStartup(IChatService chat) => _chat = chat;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Fire-and-forget so the web server starts listening even if Claude is briefly unreachable.
        // Any failure is logged; the first chat request retries the setup.
        _ = Task.Run(async () =>
        {
            try
            {
                await _chat.EnsureReadyAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                HandlerForErrors.Handle(ex);
            }
        }, cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
