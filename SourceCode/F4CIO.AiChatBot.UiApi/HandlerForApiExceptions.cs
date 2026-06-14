using System;
using System.Text.Json;
using System.Threading.Tasks;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Common.Entities;
using Microsoft.AspNetCore.Http;

namespace F4CIO.AiChatBot.UiApi;

/// <summary>
/// Converts any unhandled exception into the standard user-facing payload { message, logId }.
/// <see cref="AppException"/> already carries a built+logged Error; other exceptions are handled
/// (built + logged) here.
/// </summary>
public sealed class HandlerForApiExceptions
{
    private readonly RequestDelegate _next;

    public HandlerForApiExceptions(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException appEx)
        {
            await WriteAsync(context, appEx.Error.Message, appEx.Error.LogId);
        }
        catch (Exception ex)
        {
            var err = HandlerForErrors.Handle(ex);
            await WriteAsync(context, err.Message, err.LogId);
        }
    }

    private static async Task WriteAsync(HttpContext context, string message, string logId)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(
            new ErrorInfo { Message = message, LogId = logId },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await context.Response.WriteAsync(json);
    }
}
