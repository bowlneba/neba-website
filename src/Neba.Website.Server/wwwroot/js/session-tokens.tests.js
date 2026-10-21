// Tests for session-tokens.js
// Covers: persist, read

import { persist, read } from './session-tokens.js';

describe('session-tokens', () => {
  beforeEach(() => {
    globalThis.fetch = jest.fn();
    jest.spyOn(console, 'warn').mockImplementation(() => {});
  });

  afterEach(() => {
    jest.restoreAllMocks();
    delete globalThis.fetch;
  });

  describe('persist', () => {
    test('posts the blob as JSON without following redirects', async () => {
      globalThis.fetch.mockResolvedValue({ ok: true });

      await persist('encrypted-blob');

      expect(globalThis.fetch).toHaveBeenCalledWith('/account/session-tokens', {
        method: 'POST',
        credentials: 'same-origin',
        redirect: 'manual',
        signal: expect.any(AbortSignal),
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ blob: 'encrypted-blob' }),
      });
    });

    test('returns true when the server accepts the blob', async () => {
      globalThis.fetch.mockResolvedValue({ ok: true });

      await expect(persist('blob')).resolves.toBe(true);
    });

    test('returns false when the server rejects the blob', async () => {
      globalThis.fetch.mockResolvedValue({ ok: false });

      await expect(persist('blob')).resolves.toBe(false);
    });

    test('returns false and warns when the request fails', async () => {
      globalThis.fetch.mockRejectedValue(new Error('offline'));

      await expect(persist('blob')).resolves.toBe(false);
      expect(console.warn).toHaveBeenCalled();
    });
  });

  describe('read', () => {
    test('requests the blob without caching or following redirects', async () => {
      globalThis.fetch.mockResolvedValue({ status: 204 });

      await read();

      expect(globalThis.fetch).toHaveBeenCalledWith('/account/session-tokens', {
        credentials: 'same-origin',
        redirect: 'manual',
        cache: 'no-store',
        signal: expect.any(AbortSignal),
      });
    });

    test('returns the blob from a 200 response', async () => {
      globalThis.fetch.mockResolvedValue({ status: 200, json: () => Promise.resolve({ blob: 'encrypted-blob' }) });

      await expect(read()).resolves.toBe('encrypted-blob');
    });

    test('returns null when there is no session', async () => {
      globalThis.fetch.mockResolvedValue({ status: 204 });

      await expect(read()).resolves.toBeNull();
    });

    test('returns null for a redirect to the login page', async () => {
      globalThis.fetch.mockResolvedValue({ status: 0, type: 'opaqueredirect' });

      await expect(read()).resolves.toBeNull();
    });

    test('returns null when the body has no blob', async () => {
      globalThis.fetch.mockResolvedValue({ status: 200, json: () => Promise.resolve({}) });

      await expect(read()).resolves.toBeNull();
    });

    test('returns null and warns when the request fails', async () => {
      globalThis.fetch.mockRejectedValue(new Error('offline'));

      await expect(read()).resolves.toBeNull();
      expect(console.warn).toHaveBeenCalled();
    });
  });
});
