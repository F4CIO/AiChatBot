using System.Threading;
using System.Threading.Tasks;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Contracts;
using Microsoft.Extensions.Hosting;

namespace F4CIO.AiChatBot.UiHtmx;

public sealed class HandlerForStartup : IHostedService
{
	private readonly IChatService _chat;

	public HandlerForStartup(IChatService chat) => _chat = chat;

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				await _chat.EnsureReadyAsync(cancellationToken);
			}
			catch (System.Exception ex)
			{
				HandlerForErrors.Handle(ex);
			}
		}, cancellationToken);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
