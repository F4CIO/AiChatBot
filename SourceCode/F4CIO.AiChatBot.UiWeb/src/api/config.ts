import { Client, ErrorInfo } from './apiClient';

export { ChatRequest } from './apiClient';
export type { AppInfo, ChatResponse } from './apiClient';

declare global {
  interface Window {
    __API_BASE__?: string;
  }
}

/** Backend base URL from the runtime config (public/config.js); falls back to same-origin. */
export function apiBaseUrl(): string {
  const v = typeof window !== 'undefined' ? window.__API_BASE__ : undefined;
  return v && v.trim() && !v.includes('__API_BASE__') ? v.replace(/\/+$/, '') : '';
}

/** Singleton generated API client, pointed at the configured backend. */
export const api = new Client(apiBaseUrl());

/** Narrow a rejected value to the { message, logId } the API returns on failure. */
export function asErrorInfo(e: unknown): ErrorInfo | undefined {
  if (e instanceof ErrorInfo) return e;
  if (e && typeof e === 'object' && 'message' in e && 'logId' in e) return e as ErrorInfo;
  return undefined;
}
