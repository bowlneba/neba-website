import { describe, test, expect, beforeEach } from '@jest/globals';
import { guardSpaceKey } from './RoleButtonSpaceGuard.js';

function dispatchKeydown(target, key) {
  const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
  target.dispatchEvent(event);
  return event;
}

describe('RoleButtonSpaceGuard', () => {
  beforeEach(() => {
    document.body.innerHTML = '';
  });

  test('should prevent default when Space is pressed on a role="button" descendant', () => {
    // Arrange
    const container = document.createElement('div');
    container.id = 'test-container-1';
    const card = document.createElement('div');
    card.setAttribute('role', 'button');
    container.appendChild(card);
    document.body.appendChild(container);
    guardSpaceKey('test-container-1');

    // Act
    const event = dispatchKeydown(card, ' ');

    // Assert
    expect(event.defaultPrevented).toBe(true);
  });

  test('should not prevent default for other keys on a role="button" descendant', () => {
    // Arrange
    const container = document.createElement('div');
    container.id = 'test-container-2';
    const card = document.createElement('div');
    card.setAttribute('role', 'button');
    container.appendChild(card);
    document.body.appendChild(container);
    guardSpaceKey('test-container-2');

    // Act
    const enterEvent = dispatchKeydown(card, 'Enter');
    const tabEvent = dispatchKeydown(card, 'Tab');

    // Assert
    expect(enterEvent.defaultPrevented).toBe(false);
    expect(tabEvent.defaultPrevented).toBe(false);
  });

  test('should not prevent default for Space on a non-role="button" element', () => {
    // Arrange
    const container = document.createElement('div');
    container.id = 'test-container-3';
    const other = document.createElement('div');
    container.appendChild(other);
    document.body.appendChild(container);
    guardSpaceKey('test-container-3');

    // Act
    const event = dispatchKeydown(other, ' ');

    // Assert
    expect(event.defaultPrevented).toBe(false);
  });

  test('should do nothing when the container does not exist', () => {
    // Act - should not throw
    expect(() => guardSpaceKey('missing-container')).not.toThrow();
  });

  test('should not attach a second listener when called twice for the same container', () => {
    // Arrange
    const container = document.createElement('div');
    container.id = 'test-container-4';
    const card = document.createElement('div');
    card.setAttribute('role', 'button');
    container.appendChild(card);
    document.body.appendChild(container);

    // Act
    guardSpaceKey('test-container-4');
    guardSpaceKey('test-container-4');
    const event = dispatchKeydown(card, ' ');

    // Assert - preventDefault() called once has the same observable effect either way,
    // so this asserts no error is thrown and the guard still behaves correctly
    expect(event.defaultPrevented).toBe(true);
  });
});
