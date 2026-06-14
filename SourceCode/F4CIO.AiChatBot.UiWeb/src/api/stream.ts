import { apiBaseUrl } from './config';

/** Callbacks for a streamed chat reply — one per backend SSE frame type (meta / chunk / error / done). */
export type StreamHandlers = {
  /** First frame: the conversation id to reuse on the next turn. */
  onMeta?: (conversationId: string) => void;
  /** A piece of the reply to append to the chat as it arrives. */
  onChunk: (text: string) => void;
  /** The turn failed; message + logId to show the user. */
  onError?: (error: { message: string; logId: string }) => void;
  /** The reply finished successfully. */
  onDone?: () => void;
};

type StreamBody = { conversationId?: string; message: string };

/**
 * POST a message to the streaming endpoint and dispatch each Server-Sent Event frame to `handlers`
 * as it arrives. Used when the backend reports `useAsync`. Resolves when the stream closes.
 */
export async function streamMessage(
  body: StreamBody,
  handlers: StreamHandlers,
  signal?: AbortSignal,
): Promise<void> {
  const response = await fetch(`${apiBaseUrl()}/api/chat/messages/stream`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
    body: JSON.stringify(body),
    signal,
  });

  if (!response.ok || !response.body) {
    handlers.onError?.({ message: `Streaming request failed (HTTP ${response.status}).`, logId: '' });
    return;
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  // SSE frames are separated by a blank line; read and dispatch complete frames until the stream ends.
  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });

    let sep: number;
    while ((sep = buffer.indexOf('\n\n')) !== -1) {
      const frame = buffer.slice(0, sep);
      buffer = buffer.slice(sep + 2);
      dispatchFrame(frame, handlers);
    }
  }
}

/** Parse one `event:`/`data:` SSE frame and call the matching handler. */
function dispatchFrame(frame: string, handlers: StreamHandlers): void {
  let event = 'message';
  const dataLines: string[] = [];
  for (const line of frame.split('\n')) {
    if (line.startsWith('event:')) event = line.slice(6).trim();
    else if (line.startsWith('data:')) dataLines.push(line.slice(5).trim());
  }
  if (dataLines.length === 0) return;

  let data: any;
  try {
    data = JSON.parse(dataLines.join('\n'));
  } catch {
    return;
  }

  switch (event) {
    case 'meta':
      if (data?.conversationId) handlers.onMeta?.(data.conversationId);
      break;
    case 'chunk':
      handlers.onChunk(data?.text ?? '');
      break;
    case 'error':
      handlers.onError?.({ message: data?.message ?? 'Something went wrong.', logId: data?.logId ?? '' });
      break;
    case 'done':
      handlers.onDone?.();
      break;
  }
}
