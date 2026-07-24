import { expect, Page } from '@playwright/test';

// NebaDateInput renders month/day/year as separate segment inputs (see
// src/Neba.Website.Server/Components/NebaDateInput.razor) — `id` lands on the month segment only,
// and the segment's own JS auto-advances focus on keydown, which .fill() never fires. Fill each
// segment directly so the 'input' event listener (which does run on .fill()) reports the value.
export async function fillDateInput(page: Page, monthInputId: string, isoDate: string): Promise<void> {
  const [year, month, day] = isoDate.split('-');
  const monthInput = page.locator(`#${monthInputId}`);
  const container = monthInput.locator('xpath=..');

  await monthInput.fill(month);
  await container.locator('[data-segment="day"]').fill(day);
  await container.locator('[data-segment="year"]').fill(year);
  await expect(monthInput).toHaveValue(month);
  await expect(container.locator('[data-segment="day"]')).toHaveValue(day);
  await expect(container.locator('[data-segment="year"]')).toHaveValue(year);
}

// NebaDateTimeInput follows the same segment-per-input structure as NebaDateInput (see
// fillDateInput above), plus hour/minute/meridiem segments. The meridiem segment has no 'input'
// listener of its own (see NebaDateTimeInput.razor.js) — it's only reported to .NET when a later
// numeric segment's own 'input' event fires — so meridiem must be filled before the last numeric
// segment (minute), not after.
export async function fillDateTimeInput(page: Page, monthInputId: string, isoDateTime: string): Promise<void> {
  const [datePart, timePart] = isoDateTime.split('T');
  const [year, month, day] = datePart.split('-');
  const [hour24Str, minute] = timePart.split(':');
  const hour24 = Number.parseInt(hour24Str, 10);
  const meridiem = hour24 >= 12 ? 'PM' : 'AM';
  const hour12 = (hour24 % 12 === 0 ? 12 : hour24 % 12).toString().padStart(2, '0');

  const monthInput = page.locator(`#${monthInputId}`);
  const container = monthInput.locator('xpath=..');

  await monthInput.fill(month);
  await container.locator('[data-segment="day"]').fill(day);
  await container.locator('[data-segment="year"]').fill(year);
  await container.locator('[data-segment="hour"]').fill(hour12);
  await container.locator('[data-segment="meridiem"]').fill(meridiem);
  await container.locator('[data-segment="minute"]').fill(minute);

  await expect(monthInput).toHaveValue(month);
  await expect(container.locator('[data-segment="year"]')).toHaveValue(year);
  await expect(container.locator('[data-segment="minute"]')).toHaveValue(minute);
  await expect(container.locator('[data-segment="meridiem"]')).toHaveValue(meridiem);
}
