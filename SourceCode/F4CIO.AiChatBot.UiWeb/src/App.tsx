import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { api, asErrorInfo, ChatRequest } from './api/config';
import { streamMessage } from './api/stream';

type Msg = { role: 'user' | 'assistant'; text: string };

export default function App() {
  const [title, setTitle] = useState('F4CIO AI Chatbot');
  const [messages, setMessages] = useState<Msg[]>([]);
  const [input, setInput] = useState('');
  const [busy, setBusy] = useState(false);
  const [useAsync, setUseAsync] = useState(false);
  const [error, setError] = useState<{ message: string; logId: string } | null>(null);
  const conversationId = useRef<string | undefined>(undefined);
  const listRef = useRef<HTMLDivElement>(null);

  // Fetch the configured app title from the backend (no rebuild needed to change it).
  useEffect(() => {
    api
      .appInfo()
      .then((info) => {
        if (info?.appTitle) {
          setTitle(info.appTitle);
          document.title = info.appTitle;
        }
        setUseAsync(!!info?.useAsync);
      })
      .catch(() => {
        /* keep default title if the API is unreachable */
      });
  }, []);

  // Keep the latest message in view.
  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight, behavior: 'smooth' });
  }, [messages, busy]);

  async function send() {
    const text = input.trim();
    if (!text || busy) return;

    setError(null);
    setInput('');
    setMessages((m) => [...m, { role: 'user', text }]);
    setBusy(true);

    try {
      if (useAsync) {
        await sendStreaming(text);
      } else {
        await sendSync(text);
      }
    } catch (e) {
      const info = asErrorInfo(e);
      setError({
        message: info?.message ?? 'Something went wrong. Please try again.',
        logId: info?.logId ?? '',
      });
    } finally {
      setBusy(false);
    }
  }

  // Synchronous path: one request, the full reply arrives at once.
  async function sendSync(text: string) {
    const resp = await api.messages(
      new ChatRequest({ conversationId: conversationId.current, message: text }),
    );
    conversationId.current = resp.conversationId ?? conversationId.current;
    setMessages((m) => [...m, { role: 'assistant', text: resp.reply ?? '' }]);
  }

  // Async path: stream the reply into a single assistant bubble that grows as chunks arrive.
  async function sendStreaming(text: string) {
    await streamMessage(
      { conversationId: conversationId.current, message: text },
      {
        onMeta: (id) => {
          conversationId.current = id;
        },
        onChunk: (chunk) =>
          setMessages((m) => {
            const next = [...m];
            const last = next[next.length - 1];
            if (last && last.role === 'assistant') {
              next[next.length - 1] = { role: 'assistant', text: last.text + chunk };
            } else {
              next.push({ role: 'assistant', text: chunk });
            }
            return next;
          }),
        onError: (info) =>
          setError({
            message: info.message || 'Something went wrong. Please try again.',
            logId: info.logId,
          }),
      },
    );
  }

  function onKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      void send();
    }
  }

  return (
    <div className="chat">
      <header className="chat__header">
        <span className="chat__dot" aria-hidden="true" />
        <h1>{title}</h1>
      </header>

      <div className="chat__messages" ref={listRef}>
        {messages.length === 0 && !busy && (
          <div className="chat__empty">Ask me anything to get started.</div>
        )}

        {messages.map((m, i) => (
          <div key={i} className={`bubble bubble--${m.role}`}>
            {m.text}
          </div>
        ))}

        {busy && messages[messages.length - 1]?.role !== 'assistant' && (
          <div className="bubble bubble--assistant bubble--typing" aria-label="Assistant is typing">
            <span /><span /><span />
          </div>
        )}
      </div>

      {error && (
        <div className="chat__error" role="alert">
          <strong>Error:</strong> {error.message}
          {error.logId && <span className="chat__logid"> (LogId: {error.logId})</span>}
        </div>
      )}

      <form
        className="chat__input"
        onSubmit={(e) => {
          e.preventDefault();
          void send();
        }}
      >
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={onKeyDown}
          placeholder="Type a message…"
          rows={1}
          disabled={busy}
          autoFocus
        />
        <button type="submit" disabled={busy || !input.trim()}>
          {busy ? '…' : 'Send'}
        </button>
      </form>
    </div>
  );
}
