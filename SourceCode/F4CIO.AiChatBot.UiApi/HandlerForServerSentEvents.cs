using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace F4CIO.AiChatBot.UiApi;

/// <summary>
/// Minimal Server-Sent Events writer for the streaming chat endpoint. Each call writes one
/// <c>event:</c>/<c>data:</c> frame and flushes immediately, so every chunk reaches the browser the
/// instant it is produced. Buffering is disabled in the constructor for the same reason.
/// </summary>
public sealed class HandlerForServerSentEvents
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpResponse _response;

    public HandlerForServerSentEvents(HttpResponse response)
    {
        _response = response;
        _response.ContentType = "text/event-stream";
        _response.Headers["Cache-Control"] = "no-cache";
        _response.Headers["X-Accel-Buffering"] = "no"; // ask reverse proxies (e.g. nginx) not to buffer
        // Flush chunks as we write them instead of letting the server buffer the whole response.
        _response.HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
    }

    /// <summary>Write one SSE frame — <c>event: &lt;name&gt;</c> plus a JSON <c>data:</c> line — and flush it.</summary>
    public async Task WriteAsync(string eventName, object payload, CancellationToken ct)
    {
        var data = JsonSerializer.Serialize(payload, Json);
        var frame = $"event: {eventName}\ndata: {data}\n\n";
        await _response.WriteAsync(frame, Encoding.UTF8, ct);
        await _response.Body.FlushAsync(ct);
    }
}
