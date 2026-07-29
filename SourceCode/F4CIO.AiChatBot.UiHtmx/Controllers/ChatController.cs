using System.Text.Encodings.Web;
using System.Text.Json;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Common.Entities;
using F4CIO.AiChatBot.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace F4CIO.AiChatBot.UiHtmx.Controllers;

[Route("chat")]
public class ChatController : Controller
{
	private readonly IChatService _chat;
	public ChatController(IChatService chat) => _chat = chat;

	[HttpGet("appinfo")]
	public IActionResult AppInfo() => Ok(_chat.GetAppInfo());

	[HttpPost("send")]
	public async Task<IActionResult> Send([FromBody] ChatRequest request, CancellationToken ct)
	{
		var resp = await _chat.SendMessageAsync(request, ct);
		var encoded = HtmlEncoder.Default.Encode(resp.Reply ?? string.Empty);
		var html = $"<div class=\"bubble bubble--assistant\">{encoded}</div>";
		return Content(html, "text/html");
	}

	[HttpPost("stream")]
	public async Task Stream([FromBody] ChatRequest request)
	{
		Response.ContentType = "text/event-stream";
		Response.Headers["Cache-Control"] = "no-cache";
		Response.Headers["X-Accel-Buffering"] = "no";

		var ct = HttpContext.RequestAborted;
		try
		{
			await foreach (var chunk in _chat.StreamMessageAsync(request, ct))
			{
				if (!string.IsNullOrEmpty(chunk.ConversationId))
				{
					var data = JsonSerializer.Serialize(new { conversationId = chunk.ConversationId });
					await WriteEventAsync("meta", data, ct);
				}
				else if (chunk.Error != null)
				{
					var data = JsonSerializer.Serialize(new { message = chunk.Error.Message, logId = chunk.Error.LogId });
					await WriteEventAsync("error", data, ct);
				}
				else
				{
					var data = JsonSerializer.Serialize(chunk.Text ?? string.Empty);
					await WriteEventAsync("chunk", data, ct);
				}
			}

			await WriteEventAsync("done", "{}", ct);
		}
		catch (OperationCanceledException)
		{
			// client disconnected
		}
		catch (Exception ex)
		{
			var err = HandlerForErrors.Handle(ex);
			var data = JsonSerializer.Serialize(new { message = err.Message, logId = err.LogId });
			await WriteEventAsync("error", data, ct);
		}
	}

	private async Task WriteEventAsync(string eventName, string jsonData, CancellationToken ct)
	{
		await Response.WriteAsync($"event: {eventName}\n");
		await Response.WriteAsync($"data: {jsonData}\n\n");
		await Response.Body.FlushAsync(ct);
	}
}
