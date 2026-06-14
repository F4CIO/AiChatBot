using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AEvents = Anthropic.Models.Beta.Sessions.Events;

namespace F4CIO.AiChatBot.BusinessLogic;

/// <summary>
/// Async (streaming) half of <see cref="HandlerForClaude"/>. It sends the user message exactly like the
/// sync path (<see cref="HandlerForClaude.SendAndGetReplyAsync"/>), but <b>yields each agent message as it
/// arrives</b> instead of accumulating the whole reply first. Optionally it splits each message into
/// sentence-sized pieces (with a small pacing delay) for a smoother, progressive reveal in the UI.
/// Kept in a separate file so the existing sync logic stays untouched; it shares the same session,
/// agent and memory store via the partial class.
/// </summary>
internal sealed partial class HandlerForClaude
{
    /// <summary>
    /// Send <paramref name="userText"/> on the given session and stream the agent's reply, one piece at a
    /// time. With sentence chunking enabled each agent message is split into sentence/paragraph-sized
    /// pieces, paced by <see cref="Configuration.AddSentenceChunkingDelayInMs"/>; otherwise each whole
    /// agent message is yielded as a single piece.
    /// </summary>
    public async IAsyncEnumerable<string> StreamReplyAsync(
        string sessionId, string userText, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var sendParams = new AEvents.EventSendParams
        {
            Events = new List<AEvents.BetaManagedAgentsEventParams>
            {
                new AEvents.BetaManagedAgentsEventParams(
                    new AEvents.BetaManagedAgentsUserMessageEventParams
                    {
                        Type = AEvents.BetaManagedAgentsUserMessageEventParamsType.UserMessage,
                        Content = new List<AEvents.BetaManagedAgentsUserMessageEventParamsContent>
                        {
                            new AEvents.BetaManagedAgentsUserMessageEventParamsContent(
                                new AEvents.BetaManagedAgentsTextBlock
                                {
                                    Text = userText,
                                    Type = AEvents.BetaManagedAgentsTextBlockType.Text,
                                }, null),
                        },
                    },
                    null),
            },
        };
        await _client.Beta.Sessions.Events.Send(sessionId, sendParams, ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(3));

        await foreach (var ev in _client.Beta.Sessions.Events.StreamStreaming(
                           sessionId, new AEvents.EventStreamParams(), timeoutCts.Token))
        {
            if (ev.TryPickAgentMessageEvent(out var msg) && msg is not null)
            {
                foreach (var block in msg.Content)
                {
                    var text = block.Text;
                    if (string.IsNullOrEmpty(text)) continue;

                    if (_cfg.UseSentenceChunking)
                    {
                        foreach (var piece in SplitIntoChunks(text))
                        {
                            yield return piece;
                            if (_cfg.AddSentenceChunkingDelayInMs > 0)
                                await Task.Delay(_cfg.AddSentenceChunkingDelayInMs, timeoutCts.Token);
                        }
                    }
                    else
                    {
                        yield return text;
                    }
                }
            }
            else if (ev.TryPickSessionErrorEvent(out _))
            {
                throw new InvalidOperationException("Claude session error: " + ev.Json.GetRawText());
            }
            else if (ev.TryPickSessionStatusIdleEvent(out _) || ev.TryPickSessionStatusTerminatedEvent(out _))
            {
                break;
            }
        }
    }

    /// <summary>
    /// Split text into sentence/paragraph-sized pieces for progressive rendering. It breaks after
    /// sentence-ending punctuation (<c>.</c> <c>!</c> <c>?</c>) followed by whitespace or end-of-text,
    /// and after newlines. Every original character is preserved, so concatenating the pieces reproduces
    /// the input exactly.
    /// </summary>
    private static IEnumerable<string> SplitIntoChunks(string text)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            sb.Append(c);

            bool atSentenceEnd = (c == '.' || c == '!' || c == '?')
                                 && (i + 1 >= text.Length || char.IsWhiteSpace(text[i + 1]));
            bool atNewline = c == '\n';

            if (atSentenceEnd || atNewline)
            {
                yield return sb.ToString();
                sb.Clear();
            }
        }
        if (sb.Length > 0) yield return sb.ToString();
    }
}
