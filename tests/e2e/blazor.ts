import type { Page } from '@playwright/test';

/**
 * Opens a page and waits for its Blazor circuit to render. The page is server-rendered first, and
 * anything typed or clicked before the circuit attaches is lost.
 */
export async function gotoInteractive(page: Page, url: string): Promise<void> {
  const rendered = page
    .waitForEvent('websocket', (socket) => socket.url().includes('/_blazor'))
    .then((socket) =>
      socket.waitForEvent('framereceived', (frame) => frame.payload.toString().includes('JS.RenderBatch')));

  await page.goto(url);
  await rendered;
}
