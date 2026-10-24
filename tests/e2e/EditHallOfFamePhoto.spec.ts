import { test, expect, type Page } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

// The mock API keeps the saved photos in memory and is shared by every Playwright project, so the editing
// flows run on desktop only and start from the initial data.
test.skip(({ isMobile }) => isMobile, 'Photo editing is covered on desktop; the mock API state is shared across projects.');

test.beforeAll(async ({ playwright }) => {
  const api = await playwright.request.newContext();
  await api.post('http://localhost:5151/__mock/hall-of-fame/reset');
  await api.dispose();
});

const PHOTO_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
  'base64'
);

const JANE = { name: 'Jane Smith', id: '01JX0000000000000000000001' };
const BOB = { name: 'Bob Johnson', id: '01JX0000000000000000000002' };
const ALICE = { name: 'Alice Williams', id: '01JX0000000000000000000003' };
const TOM = { name: 'Tom Davis', id: '01JX0000000000000000000004' };

async function openEditor(page: Page, inductee: { name: string }) {
  await page.getByRole('button', { name: `Edit photo for ${inductee.name}` }).click();
  await expect(page.getByRole('dialog')).toContainText('Edit photo');
  await expect(page.locator('.ehp-subtitle')).toContainText(inductee.name);
}

async function uploadPhoto(page: Page) {
  await page.locator('input.neba-file-upload-input').setInputFiles({
    name: 'photo.png',
    mimeType: 'image/png',
    buffer: PHOTO_PNG,
  });
  await expect(page.locator('.neba-file-upload-item-status--success')).toBeVisible();
}

async function lastPut(page: Page, id: string) {
  return page.request
    .get(`http://localhost:5151/__mock/last-request?method=PUT&path=/hall-of-fame/${id}/photo`)
    .then((response) => response.json());
}

function avatarOf(page: Page, inductee: { name: string }) {
  return page.locator('.inductee-item', { has: page.locator('.inductee-name', { hasText: inductee.name }) });
}

test.describe('Edit Hall of Fame photo (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the edit-photo buttons', async ({ page }) => {
    await page.goto('/hall-of-fame');
    await page.waitForSelector('.inductee-item');

    await expect(page.locator('button.hof-photo-edit')).toHaveCount(0);
  });
});

test.describe('Edit Hall of Fame photo (with the permission)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=HallOfFame.EditHallOfFamePhoto');
    await page.goto('/hall-of-fame');
    await page.waitForSelector('.inductee-item');
  });

  test('shows an edit-photo button for each inductee', async ({ page }) => {
    await expect(page.locator('button.hof-photo-edit')).toHaveCount(4);
  });

  test('uploads a photo, zooms, places the focal point, and shows it on the page', async ({ page }) => {
    await openEditor(page, JANE);
    await expect(page.getByRole('button', { name: 'Save photo' })).toBeDisabled();
    await expect(page.locator('.ehp-empty__box')).toContainText('No photo yet');

    await uploadPhoto(page);
    await expect(page.locator('.ehp-badge')).toContainText('New photo');
    await expect(page.getByRole('button', { name: 'Save photo' })).toBeEnabled();

    // Wait for the photo to load so the picker has measured its size, then zoom in so the window can move.
    await page.waitForFunction(() => {
      const image = document.querySelector<HTMLImageElement>('[data-fp-surface] img');
      return !!image && image.naturalWidth > 0;
    });
    await page.getByLabel('Zoom').fill('200');
    const box = (await page.locator('.focal-picker__surface').boundingBox())!;
    await page.mouse.click(box.x + box.width * 0.25, box.y + box.height * 0.25);

    await page.getByRole('button', { name: 'Save photo' }).click();
    await expect(page.locator('.neba-toast')).toContainText('Photo Saved');
    await expect(page.locator('.ehp-subtitle')).toHaveCount(0);

    const request = await lastPut(page, JANE.id);
    expect(request.input.photo.path).toBe('hall-of-fame/photo/e2e-test-photo.png');
    expect(request.input.zoom).toBe(200);
    expect(request.input.focalX).toBe(10);
    expect(request.input.focalY).toBe(0);

    const image = avatarOf(page, JANE).locator('img.inductee-photo__img');
    await expect(image).toHaveAttribute('style', /scale\(2\)/);
    await expect(image).toHaveAttribute('style', /object-position:10% 0%/);
  });

  test('moves the focal point of the current photo and sends a null photo', async ({ page }) => {
    await openEditor(page, BOB);
    await expect(page.locator('.ehp-state')).toContainText('Current photo');
    await expect(page.getByRole('button', { name: 'Save photo' })).toBeDisabled();

    await page.locator('.focal-picker__surface').focus();
    await page.keyboard.press('Shift+ArrowRight');
    await expect(page.getByRole('button', { name: 'Save photo' })).toBeEnabled();

    await page.getByRole('button', { name: 'Save photo' }).click();
    await expect(page.locator('.neba-toast')).toContainText('Photo Saved');

    const request = await lastPut(page, BOB.id);
    expect(request.input.photo).toBeNull();
    expect(request.input.focalX).toBe(25);
    expect(request.input.focalY).toBe(50);
    expect(request.input.zoom).toBe(100);

    await expect(avatarOf(page, BOB).locator('img.inductee-photo__img')).toHaveAttribute('style', /object-position:25% 50%/);
  });

  test('removes a photo and shows the initials again', async ({ page }) => {
    await expect(avatarOf(page, ALICE).locator('img.inductee-photo__img')).toHaveCount(1);

    await openEditor(page, ALICE);
    await page.getByRole('button', { name: 'Remove photo' }).click();
    await expect(page.locator('.ehp-empty__box')).toContainText('Photo will be removed');

    await page.getByRole('button', { name: 'Save photo' }).click();
    await expect(page.locator('.neba-toast')).toContainText('Photo Saved');

    const request = await lastPut(page, ALICE.id);
    expect(request.input.photo).toBeNull();
    expect(request.input.focalX).toBeNull();

    await expect(avatarOf(page, ALICE).locator('img.inductee-photo__img')).toHaveCount(0);
    await expect(avatarOf(page, ALICE).locator('.inductee-photo')).toContainText('AW');
  });

  test('asks before discarding an uploaded photo when Cancel is clicked', async ({ page }) => {
    await openEditor(page, TOM);
    await uploadPhoto(page);

    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(page.getByText('Discard unsaved changes?')).toBeVisible();

    await page.getByRole('button', { name: 'Discard', exact: true }).click();
    await expect(page.locator('.ehp-subtitle')).toHaveCount(0);
    await expect(avatarOf(page, TOM).locator('img.inductee-photo__img')).toHaveCount(0);
  });

  test('shows the error and keeps the editor open when the save fails', async ({ page }) => {
    await openEditor(page, TOM);
    await uploadPhoto(page);

    await page.request.post(`http://localhost:5151/__mock/fail?path=/hall-of-fame/${TOM.id}/photo&status=500`);
    try {
      await page.getByRole('button', { name: 'Save photo' }).click();

      await expect(page.getByText("Couldn't Save Photo")).toBeVisible();
      await expect(page.getByRole('dialog')).toContainText('Edit photo');
      await expect(page.getByRole('button', { name: 'Save photo' })).toBeEnabled();
    } finally {
      await page.request.post(`http://localhost:5151/__mock/reset?path=/hall-of-fame/${TOM.id}/photo`);
    }

    await expect(avatarOf(page, TOM).locator('img.inductee-photo__img')).toHaveCount(0);
  });

  test('shows the error when the upload fails', async ({ page }) => {
    await openEditor(page, TOM);

    await page.request.post('http://localhost:5151/__mock/fail?path=/hall-of-fame/photos&status=500');
    try {
      await page.locator('input.neba-file-upload-input').setInputFiles({
        name: 'photo.png',
        mimeType: 'image/png',
        buffer: PHOTO_PNG,
      });

      await expect(page.locator('.neba-file-upload-item-status--error')).toBeVisible();
      await expect(page.getByRole('button', { name: 'Save photo' })).toBeDisabled();
    } finally {
      await page.request.post('http://localhost:5151/__mock/reset?path=/hall-of-fame/photos');
    }
  });
});
