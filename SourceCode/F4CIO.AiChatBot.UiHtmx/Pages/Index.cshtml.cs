using F4CIO.AiChatBot.Contracts;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace F4CIO.AiChatBot.UiHtmx.Pages;

public class IndexModel : PageModel
{
	private readonly IChatService _chat;
	public string AppTitle { get; private set; } = "F4CIO AI Chatbot";
	public bool UseAsync { get; private set; }

	public IndexModel(IChatService chat) => _chat = chat;

	public void OnGet()
	{
		try
		{
			var info = _chat.GetAppInfo();
			if (!string.IsNullOrWhiteSpace(info?.AppTitle)) AppTitle = info.AppTitle!;
			UseAsync = info?.UseAsync ?? false;
		}
		catch
		{
			// keep defaults if BusinessLogic is unavailable
		}
	}
}
