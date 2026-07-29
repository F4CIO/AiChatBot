(() => {
  const decoder = new TextDecoder();

  function createBubble(role, text) {
    const div = document.createElement('div');
    div.className = 'bubble ' + (role === 'user' ? 'bubble--user' : 'bubble--assistant');
    div.textContent = text;
    return div;
  }

  function createTypingIndicator() {
    const div = document.createElement('div');
    div.className = 'bubble bubble--assistant bubble--typing';
    div.setAttribute('aria-label', 'Assistant is typing');
    div.innerHTML = '<span></span><span></span><span></span>';
    return div;
  }

  async function postStream(request, onMeta, onChunk, onDone, onError) {
    try {
      const base = (window.__APP_BASE__ || '/').replace(/\/+$/, '');
      const resp = await fetch(base + '/chat/stream', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify(request),
      });
      if (!resp.ok) {
        const err = await resp.json().catch(() => null);
        onError?.(err ?? { message: 'Stream failed' });
        return;
      }

      const reader = resp.body.getReader();
      let buf = '';
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        buf += decoder.decode(value, { stream: true });
        let idx;
        while ((idx = buf.indexOf('\n\n')) !== -1) {
          const block = buf.slice(0, idx).trim();
          buf = buf.slice(idx + 2);
          if (!block) continue;
          // parse lines
          const lines = block.split(/\r?\n/);
          let ev = null;
          let data = '';
          for (const line of lines) {
            if (line.startsWith('event:')) ev = line.slice(6).trim();
            else if (line.startsWith('data:')) data += line.slice(5).trim();
          }
          if (ev === 'meta') {
            const obj = JSON.parse(data || '{}');
            onMeta?.(obj.conversationId);
          } else if (ev === 'chunk') {
            // data is JSON string for the chunk text
            try { const txt = JSON.parse(data); onChunk?.(txt); } catch { onChunk?.(data); }
          } else if (ev === 'done') { onDone?.(); }
          else if (ev === 'error') { const obj = JSON.parse(data || '{}'); onError?.(obj); }
        }
      }
      onDone?.();
    } catch (ex) {
      onError?.({ message: ex?.message ?? String(ex) });
    }
  }

  document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('chatForm');
    const input = document.getElementById('input');
    const messages = document.getElementById('messages');
    const errorEl = document.getElementById('error');
    const sendBtn = document.getElementById('sendBtn');
    let conversationId = undefined;

    function setError(info) {
      if (!info) { errorEl.style.display = 'none'; return; }
      errorEl.style.display = 'block';
      errorEl.textContent = 'Error: ' + (info.message || 'Something went wrong.');
      if (info.logId) {
        const span = document.createElement('span'); span.className = 'chat__logid'; span.textContent = ' (LogId: ' + info.logId + ')';
        errorEl.appendChild(span);
      }
    }

    async function sendSync(text) {
      const req = { conversationId, message: text };
      sendBtn.disabled = true; input.disabled = true;
      try {
        const base = (window.__APP_BASE__ || '/').replace(/\/+$/, '');
        const resp = await fetch(base + '/chat/send', {
          method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(req)
        });
        if (!resp.ok) {
          const err = await resp.json().catch(() => null);
          setError(err ?? { message: 'Send failed' });
          return;
        }
        const html = await resp.text();
        // append user bubble and assistant bubble
        messages.querySelector('.chat__empty')?.remove();
        messages.appendChild((() => { const d=document.createElement('div'); d.innerHTML = '<div class="bubble bubble--user">'+text+'</div>'; return d.firstChild; })());
        const temp = document.createElement('div'); temp.innerHTML = html; messages.appendChild(temp.firstChild);
      } finally { sendBtn.disabled = false; input.disabled = false; }
    }

    async function sendStream(text) {
      const req = { conversationId, message: text };
      setError(null);
      messages.querySelector('.chat__empty')?.remove();
      // append user bubble
      messages.appendChild(createBubble('user', text));
      const typing = createTypingIndicator(); messages.appendChild(typing);
      sendBtn.disabled = true; input.disabled = true;

      let assistantBubble = null;

      await postStream(req,
        (metaId) => { if (metaId) conversationId = metaId; },
        (chunk) => {
          if (!assistantBubble) {
            assistantBubble = createBubble('assistant', ''); messages.appendChild(assistantBubble);
          }
          assistantBubble.textContent = (assistantBubble.textContent || '') + chunk;
          messages.scrollTo({ top: messages.scrollHeight });
        },
        () => {
          typing.remove(); sendBtn.disabled = false; input.disabled = false; input.value = '';
        },
        (err) => { typing.remove(); sendBtn.disabled = false; input.disabled = false; setError(err); }
      );
    }

    // Submit on Enter (without Shift) like the React UI.
    input.addEventListener('keydown', (e) => {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        // Use requestSubmit so the form's submit handler runs and validation occurs.
        if (typeof form.requestSubmit === 'function') form.requestSubmit();
        else form.dispatchEvent(new Event('submit', { cancelable: true, bubbles: true }));
      }
    });

    form.addEventListener('submit', (e) => {
      e.preventDefault();
      const text = input.value.trim(); if (!text) return;
      const useAsync = window.__USE_ASYNC__ === true || window.__USE_ASYNC__ === 'true';
      if (useAsync) {
        void sendStream(text);
      } else {
        void sendSync(text);
        input.value = '';
      }
    });
  });

})();
