// Unit tests for breakpoints.js
// Ensures breakpoint constants are properly defined and synchronized

import { BREAKPOINTS } from './breakpoints.js';

describe('BREAKPOINTS', () => {
  test('should export all required breakpoint constants', () => {
    expect(BREAKPOINTS).toBeDefined();
    expect(typeof BREAKPOINTS).toBe('object');
  });

  test('should have MOBILE breakpoint defined', () => {
    expect(BREAKPOINTS.MOBILE).toBe(767);
    expect(typeof BREAKPOINTS.MOBILE).toBe('number');
  });

  test('should have TABLET_MIN breakpoint defined', () => {
    expect(BREAKPOINTS.TABLET_MIN).toBe(768);
    expect(typeof BREAKPOINTS.TABLET_MIN).toBe('number');
  });

  test('should have TABLET_MAX breakpoint defined', () => {
    expect(BREAKPOINTS.TABLET_MAX).toBe(1100);
    expect(typeof BREAKPOINTS.TABLET_MAX).toBe('number');
  });

  test('should have DESKTOP_TIGHT_MIN breakpoint defined', () => {
    expect(BREAKPOINTS.DESKTOP_TIGHT_MIN).toBe(1101);
    expect(typeof BREAKPOINTS.DESKTOP_TIGHT_MIN).toBe('number');
  });

  test('should have DESKTOP_TIGHT_MAX breakpoint defined', () => {
    expect(BREAKPOINTS.DESKTOP_TIGHT_MAX).toBe(1250);
    expect(typeof BREAKPOINTS.DESKTOP_TIGHT_MAX).toBe('number');
  });

  test('should have DESKTOP_MEDIUM_MIN breakpoint defined', () => {
    expect(BREAKPOINTS.DESKTOP_MEDIUM_MIN).toBe(1251);
    expect(typeof BREAKPOINTS.DESKTOP_MEDIUM_MIN).toBe('number');
  });

  test('should have DESKTOP_MEDIUM_MAX breakpoint defined', () => {
    expect(BREAKPOINTS.DESKTOP_MEDIUM_MAX).toBe(1400);
    expect(typeof BREAKPOINTS.DESKTOP_MEDIUM_MAX).toBe('number');
  });

  test('should have DESKTOP_WIDE_MIN breakpoint defined', () => {
    expect(BREAKPOINTS.DESKTOP_WIDE_MIN).toBe(1401);
    expect(typeof BREAKPOINTS.DESKTOP_WIDE_MIN).toBe('number');
  });

  test('should have logical breakpoint relationships', () => {
    // Mobile should be less than tablet min
    expect(BREAKPOINTS.MOBILE).toBeLessThan(BREAKPOINTS.TABLET_MIN);

    // Tablet min should be less than tablet max
    expect(BREAKPOINTS.TABLET_MIN).toBeLessThan(BREAKPOINTS.TABLET_MAX);

    // Tablet max should be less than desktop tight min
    expect(BREAKPOINTS.TABLET_MAX).toBeLessThan(BREAKPOINTS.DESKTOP_TIGHT_MIN);

    // Desktop tight min should be less than desktop tight max
    expect(BREAKPOINTS.DESKTOP_TIGHT_MIN).toBeLessThan(BREAKPOINTS.DESKTOP_TIGHT_MAX);

    // Desktop tight max should be less than desktop medium min
    expect(BREAKPOINTS.DESKTOP_TIGHT_MAX).toBeLessThan(BREAKPOINTS.DESKTOP_MEDIUM_MIN);

    // Desktop medium min should be less than desktop medium max
    expect(BREAKPOINTS.DESKTOP_MEDIUM_MIN).toBeLessThan(BREAKPOINTS.DESKTOP_MEDIUM_MAX);

    // Desktop medium max should be less than desktop wide min
    expect(BREAKPOINTS.DESKTOP_MEDIUM_MAX).toBeLessThan(BREAKPOINTS.DESKTOP_WIDE_MIN);
  });

  test('should have no duplicate breakpoint values', () => {
    const values = Object.values(BREAKPOINTS);
    const uniqueValues = new Set(values);
    expect(uniqueValues.size).toBe(values.length);
  });

  test('should have all positive breakpoint values', () => {
    Object.values(BREAKPOINTS).forEach(value => {
      expect(value).toBeGreaterThan(0);
    });
  });
});